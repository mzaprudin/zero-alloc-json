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
| `System.Text.Json · new object` | 574.20 ns | 5.23 ns | 120 B | 1.00× |
| `JsonPopulator · reused object` | 537.89 ns | 4.90 ns | 0 B | 1.07× |
| `JsonPopulator · struct (ref)` | 471.62 ns | 4.62 ns | 0 B | 1.22× |
| `System.Text.Json · new contract list` | 1.80 ms | 16.76 µs | 141408 B | 1.00× |
| `JsonPopulator · FastList<class>` | 1.53 ms | 12.48 µs | 0 B | 1.18× |
| `JsonPopulator · FastList<struct>` | 1.46 ms | 9.89 µs | 0 B | 1.23× |

The reused-object and FastList cases use warmed interning. The trade class includes Symbol; the struct omits it. Speedup uses a separate System.Text.Json baseline for one object and for the contract list.

### JSON writing

Fresh output arrays, reusable writers and direct field-by-field writing. Class and struct models match the population workloads above.

| Method | Mean | Error | Allocated | Speedup |
|---|---:|---:|---:|---:|
| `JsonSerializer → new byte[]` | 392.77 ns | 2.37 ns | 192 B | 1.00× |
| `JsonSerializer → reused writer` | 364.99 ns | 2.31 ns | 0 B | 1.00× |
| `JsonPushulator · class` | 295.35 ns | 1.30 ns | 0 B | 1.24× |
| `JsonPushulator · struct` | 273.27 ns | 1.77 ns | 0 B | 1.34× |
| `Utf8JsonWriter · manual` | 302.87 ns | 1.50 ns | 0 B | 1.21× |

Writer speedup is relative to JsonSerializer with a reused writer; the fresh-array row uses itself as baseline. Caller-owned buffers keep all reused-writer variants at 0 B/op.

### Symbol routing from UTF-8

Look up the symbol in each compact message without building an object tree. The workload covers 5,000 symbols; values below are per message.

| Method | Mean | Error | Allocated | Speedup |
|---|---:|---:|---:|---:|
| `Utf8JsonReader` | 81.53 ns | 0.43 ns | 0 B | 1.00× |
| `Span.IndexOf` | 47.89 ns | 0.26 ns | 0 B | 1.70× |
| `Regex` | 93.77 ns | 0.66 ns | 0 B | 0.87× |

Span.IndexOf searches the compact ASCII symbol field. The accepted message shape and multi-segment behavior are described in the architecture notes.

### Same-model parsing

All variants read the same seven-field trade, including Symbol. These rows separate object reuse from string interning.

| Method | Mean | Error | Allocated | Speedup |
|---|---:|---:|---:|---:|
| `System.Text.Json` | 521.36 ns | 7.42 ns | 120 B | 1.00× |
| `System.Text.Json · generated` | 563.29 ns | 3.87 ns | 120 B | 0.93× |
| `JsonPopulator · new object` | 493.52 ns | 5.49 ns | 120 B | 1.06× |
| `JsonPopulator · reused object` | 432.74 ns | 3.68 ns | 40 B | 1.20× |
| `JsonPopulator · reuse + interning` | 430.47 ns | 5.20 ns | 0 B | 1.21× |

Reusing only the object still allocates its Symbol string. Reusing the object and an already interned symbol reaches 0 B/op.

### Same-model writing

The same trade, output buffer and writer lifetime, including the source-generated System.Text.Json baseline.

| Method | Mean | Error | Allocated | Speedup |
|---|---:|---:|---:|---:|
| `System.Text.Json` | 357.00 ns | 2.50 ns | 0 B | 1.00× |
| `System.Text.Json · generated` | 268.14 ns | 1.38 ns | 0 B | 1.33× |
| `JsonPushulator → reused writer` | 308.00 ns | 2.45 ns | 0 B | 1.16× |

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
