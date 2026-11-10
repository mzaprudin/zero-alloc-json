# zero-alloc-json

[English](README.md) · [Русский](README.ru.md) · [CI](https://github.com/mzaprudin/zero-alloc-json/actions/workflows/ci.yml)

Zero-allocation JSON population, writing, reusable collections, string interning and UTF-8 symbol routing for C# / .NET 10. Reusing objects and buffers reduces GC pressure and removes per-message allocations as a source of unpredictable pauses in continuous data processing.

## Run it

```sh
dotnet restore ZeroAlloc.Json.sln --locked-mode
dotnet build ZeroAlloc.Json.sln -c Release --no-restore
dotnet test tests/ZeroAlloc.Json.Tests -c Release --no-build
dotnet run -c Release --no-build --project samples/ZeroAlloc.Json.Demo
```

Requires the .NET 10 SDK. All **56 original tests** are retained. The example checks every parsed field and compares written bytes with the framework serializer.

## Fresh local measurements

AMD Ryzen 9 5950X · Ubuntu 24.04 · .NET 10.0.14 · BenchmarkDotNet 0.15.8. Release/x64; 5 warmups, 15 measurement iterations with a 500 ms target, one launch per case.

Speedup = standard mean / method mean, with separate baselines for the DTO and contract list. The DTO includes Symbol; the original struct does not. The list contains 592 contracts. Metadata creation, collection growth and initial interning precede measurement. Allocation columns show actual bytes per operation.

Writing speedup uses the standard reused writer; the fresh byte[] row uses itself as baseline.

<!-- LOCAL_RESULTS_START -->
### JSON population

| Method | Mean | Error (99.9% CI) | Allocated B/op | Speedup |
|---|---:|---:|---:|---:|
| `Standard_Deserialize_Class` | 574.20 ns | 5.23 ns | 120 B | 1.00× |
| `JsonPopulator_Class_Reused_Interned` | 537.89 ns | 4.90 ns | 0 B | 1.07× |
| `JsonPopulator_Struct_Ref` | 471.62 ns | 4.62 ns | 0 B | 1.22× |
| `Gate_Standard_Deserialize_List` | 1.80 ms | 16.76 µs | 141408 B | 1.00× |
| `Gate_JsonPopulator_FastList_Reused_Interned` | 1.53 ms | 12.48 µs | 0 B | 1.18× |
| `Gate_JsonPopulator_FastList_Reused_Interned_Struct` | 1.46 ms | 9.89 µs | 0 B | 1.23× |

### JSON writing

| Method | Mean | Error (99.9% CI) | Allocated B/op | Speedup |
|---|---:|---:|---:|---:|
| `Standard_Serialize_Class` | 392.77 ns | 2.37 ns | 192 B | 1.00× |
| `Standard_Serialize_Writer` | 364.99 ns | 2.31 ns | 0 B | 1.00× |
| `JsonPushulator_Class` | 295.35 ns | 1.30 ns | 0 B | 1.24× |
| `JsonPushulator_Struct` | 273.27 ns | 1.77 ns | 0 B | 1.34× |
| `Manual_Utf8JsonWriter_Direct` | 302.87 ns | 1.50 ns | 0 B | 1.21× |

### UTF-8 routing

| Method | Mean | Error (99.9% CI) | Allocated B/op | Speedup |
|---|---:|---:|---:|---:|
| `Route10kMessages_JsonReader` | 81.53 ns | 0.43 ns | 0 B | 1.00× |
| `Route10kMessages_Custom` | 47.89 ns | 0.26 ns | 0 B | 1.70× |
| `Route10kMessages_Regex` | 93.77 ns | 0.66 ns | 0 B | 0.87× |

### Equal-model parsing

| Method | Mean | Error (99.9% CI) | Allocated B/op | Speedup |
|---|---:|---:|---:|---:|
| `SystemTextJson` | 521.36 ns | 7.42 ns | 120 B | 1.00× |
| `SystemTextJsonGenerated` | 563.29 ns | 3.87 ns | 120 B | 0.93× |
| `PopulatorNewObject` | 493.52 ns | 5.49 ns | 120 B | 1.06× |
| `PopulatorReusedObject` | 432.74 ns | 3.68 ns | 40 B | 1.20× |
| `PopulatorReusedInterned` | 430.47 ns | 5.20 ns | 0 B | 1.21× |

### Equal-model writing

| Method | Mean | Error (99.9% CI) | Allocated B/op | Speedup |
|---|---:|---:|---:|---:|
| `SystemTextJson` | 357.00 ns | 2.50 ns | 0 B | 1.00× |
| `SystemTextJsonGenerated` | 268.14 ns | 1.38 ns | 0 B | 1.33× |
| `PushulatorReusedWriter` | 308.00 ns | 2.45 ns | 0 B | 1.16× |

<!-- LOCAL_RESULTS_END -->

[Complete reports, raw samples and before/after comparison](results/local/README.md) · [Methodology](docs/benchmark-methodology.md)

```sh
python3 scripts/run-benchmarks.py
python3 scripts/render-results.py results/local/RUN_DIRECTORY
```

## Architecture and verification

[Memory ownership and limits](docs/architecture.md) · [Build and CI](docs/development.md) · [Verification](docs/verification.md) · [Provenance](docs/provenance.md)

The standalone `ZeroAlloc.Json` assembly preserves GapSonar namespaces. It has no dependency on [pipelines-websocket](https://github.com/mzaprudin/pipelines-websocket); both libraries can be referenced together.

Published for portfolio review without an open-source license. See [NOTICE](NOTICE).
