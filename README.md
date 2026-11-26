<p align="center"><strong>English</strong> · <a href="README.ru.md">Русский</a> · <a href="https://github.com/mzaprudin/zero-alloc-json/actions/workflows/ci.yml">CI</a></p>

<h1><img src="assets/readme/hero-en.svg" width="100%" alt="zero-alloc-json — JSON for low-latency systems"></h1>

C# / .NET 10 components for low-latency JSON stream processing. Optimized parsing reduces processing time, while reused objects and buffers reduce garbage-collection pressure. This helps keep latency predictable and the system stable under continuous data load.

**0 B/op in the warmed reuse scenarios.** Metadata, capacity and interned symbols are prepared before measurement; initialization, growth and new strings allocate.

[Benchmarks](#json-and-routing-benchmarks) · [Reuse example](#reuse-in-practice) · [Build and run](#build-and-run-the-example)

## JSON and routing benchmarks

AMD Ryzen 9 5950X · Ubuntu 24.04 · .NET 10.0.14 · BenchmarkDotNet 0.15.8 · Release/x64. Five warmup and fifteen measurement iterations with a 500 ms target, one launch per case.

<!-- LOCAL_RESULTS_START -->
**Reuse + interning: 0 B/op · Reused writer: 0 B/op · Routing: 1.70×.**

### JSON population

One trade update and a list of 592 contracts. Objects, collection capacity and interned symbols are prepared before timing.

| Method | Mean | Error | Allocated | Speedup |
|---|---:|---:|---:|---:|
| `System.Text.Json · new object` | 577.22 ns | 5.51 ns | 120 B | 1.00× |
| `JsonPopulator · reused object` | 538.96 ns | 3.12 ns | 0 B | 1.07× |
| `JsonPopulator · struct (ref)` | 458.84 ns | 5.16 ns | 0 B | 1.26× |
| `System.Text.Json · new contract list` | 1.80 ms | 10.87 µs | 141408 B | 1.00× |
| `JsonPopulator · FastList<class>` | 1.51 ms | 9.47 µs | 0 B | 1.19× |
| `JsonPopulator · FastList<struct>` | 1.45 ms | 13.70 µs | 0 B | 1.24× |

The reused-object and FastList cases use warmed interning. The trade class includes Symbol; the struct omits it. Speedup uses a separate System.Text.Json baseline for one object and for the contract list.

### JSON writing

Fresh output arrays, reusable writers and direct field-by-field writing. Class and struct models match the population workloads above.

| Method | Mean | Error | Allocated | Speedup |
|---|---:|---:|---:|---:|
| `JsonSerializer → new byte[]` | 406.66 ns | 2.70 ns | 192 B | 1.00× |
| `JsonSerializer → reused writer` | 342.32 ns | 2.14 ns | 0 B | 1.00× |
| `JsonPushulator · class` | 306.83 ns | 2.78 ns | 0 B | 1.12× |
| `JsonPushulator · struct` | 262.68 ns | 1.51 ns | 0 B | 1.30× |
| `Utf8JsonWriter · manual` | 303.05 ns | 2.23 ns | 0 B | 1.13× |

Writer speedup is relative to JsonSerializer with a reused writer; the fresh-array row uses itself as baseline. Caller-owned buffers keep all reused-writer variants at 0 B/op.

### Symbol routing from UTF-8

Look up the symbol in each compact message without building an object tree. The workload covers 5,000 symbols; values below are per message.

| Method | Mean | Error | Allocated | Speedup |
|---|---:|---:|---:|---:|
| `Utf8JsonReader` | 82.00 ns | 0.14 ns | 0 B | 1.00× |
| `Span.IndexOf` | 48.24 ns | 0.24 ns | 0 B | 1.70× |
| `Regex` | 111.60 ns | 0.33 ns | 0 B | 0.73× |

Span.IndexOf searches the compact ASCII symbol field. The accepted message shape and multi-segment behavior are described in the architecture notes.

### Same-model parsing

All variants read the same seven-field trade, including Symbol. These rows separate object reuse from string interning.

| Method | Mean | Error | Allocated | Speedup |
|---|---:|---:|---:|---:|
| `System.Text.Json` | 530.64 ns | 4.04 ns | 120 B | 1.00× |
| `System.Text.Json · generated` | 503.30 ns | 6.68 ns | 120 B | 1.05× |
| `JsonPopulator · new object` | 476.48 ns | 3.00 ns | 120 B | 1.11× |
| `JsonPopulator · reused object` | 432.69 ns | 3.17 ns | 40 B | 1.23× |
| `JsonPopulator · reuse + interning` | 431.64 ns | 3.60 ns | 0 B | 1.23× |

Reusing only the object still allocates its Symbol string. Reusing the object and an already interned symbol reaches 0 B/op.

### Same-model writing

The same trade, output buffer and writer lifetime, including the source-generated System.Text.Json baseline.

| Method | Mean | Error | Allocated | Speedup |
|---|---:|---:|---:|---:|
| `System.Text.Json` | 347.13 ns | 1.83 ns | 0 B | 1.00× |
| `System.Text.Json · generated` | 268.85 ns | 2.54 ns | 0 B | 1.29× |
| `JsonPushulator → reused writer` | 310.22 ns | 1.63 ns | 0 B | 1.12× |

The source-generated framework writer is fastest here; all three variants allocate 0 B/op. Population of existing objects and reusable collections is covered separately above.

<!-- LOCAL_RESULTS_END -->

Allocated memory is managed bytes per operation; routing is normalized per message. Error is the half-width of the 99.9% confidence interval. Speedup is the corresponding standard mean divided by the method mean.

[Raw reports and samples](results/local/20261110T182522Z/bdn/results/) · [Workloads and reproduction](docs/benchmark-methodology.md)

## Reuse in practice

The destination is created once. Here, `intern` is a cached interning delegate and `writer` is a reusable `Utf8JsonWriter`:

```csharp
var trade = new Trade();
JsonPopulator.Populate(firstUpdate, trade, intern);
JsonPopulator.Populate(nextUpdate, trade, intern); // Update the same object.
JsonPushulator.Push(trade, writer);
writer.Flush();
```

[Complete parsing and writing example](samples/ZeroAlloc.Json.Demo/Program.cs) · [Ownership and parser behavior](docs/architecture.md)

### Engineering decisions

- **Reuse destinations.** `JsonPopulator` updates prepared classes, structs and `FastList<T>` slots. Partial updates retain fields absent from the message.
- **Prepare access once.** Member lookup and compiled writers are cached per type. The caller reuses `Utf8JsonWriter` and its output buffer.
- **Reuse known strings.** Optional interning shares recurring symbols; fixed-size strings provide another way to avoid managed string allocation.
- **Route the incoming bytes.** The compact-message router finds the symbol in UTF-8 and resolves it without building an object tree.

## Build and run the example

Requires the .NET 10 SDK:

```sh
dotnet restore ZeroAlloc.Json.sln --locked-mode
dotnet build ZeroAlloc.Json.sln -c Release --no-restore
dotnet test tests/ZeroAlloc.Json.Tests -c Release --no-build
dotnet run -c Release --no-build --project samples/ZeroAlloc.Json.Demo
```

The example checks parsed fields and serialized bytes against System.Text.Json. Tests exercise reusable destinations, escaped and segmented input, numeric strings, GUIDs and writer output. Linux and Windows CI run the tests, example and every benchmark setup.

## Documentation

[Architecture and memory ownership](docs/architecture.md) · [Development](docs/development.md) · [Verification](docs/verification.md) · [Source and measurement provenance](docs/provenance.md)

Published for portfolio review without an open-source license. See [NOTICE](NOTICE).
