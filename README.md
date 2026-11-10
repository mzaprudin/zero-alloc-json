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

AMD Ryzen 9 5950X · Ubuntu 24.04 · .NET 10.0.14 · BenchmarkDotNet 0.15.8. Release/x64; 5 warmups, 15 measurement iterations with a 500 ms target, one launch.

Speedup = standard mean / method mean, with separate baselines for the DTO and contract list. The DTO includes Symbol; the original struct does not. The list contains 592 contracts. Metadata creation, collection growth and initial interning precede measurement. Allocation columns show actual bytes per operation.

<!-- LOCAL_RESULTS_START -->
Measurements are being prepared.
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
