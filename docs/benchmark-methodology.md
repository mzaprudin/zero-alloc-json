# Benchmark methodology

[English](benchmark-methodology.md) · [Русский](benchmark-methodology.ru.md)

The pinned upstream revision and split assemblies are measured sequentially on the same machine: Release/x64, 5 warmup and 15 measurement iterations targeting 500 ms, one launch. Error is the half-width of the BenchmarkDotNet 99.9% confidence interval. BenchmarkDotNet handles outliers; full samples, intervals, GC and allocations are retained in JSON. Iteration time is a target rather than a hard duration.

22 cases: 6 original population, 5 writing, 3 routing, plus 8 equal-model cases. The Gate fixture contains 592 contracts and 1,000,081 bytes. Original DTO and struct differ by Symbol; writing scenarios distinguish new arrays from reused writers. Additional cases compare identical models and buffer lifecycles, including source-generated System.Text.Json.

Routing handles 10,000 messages per invocation with OperationsPerInvoke = 10000; table time and memory are per message. Setup builds 5,000 symbols using seed 42. The fast router assumes a compact ASCII field; that scope is preserved.

Setup validates DTO fields, every Gate contract, class/struct writer output and every route before timing. Metadata, writers, interning and collection capacity are warmed. 0 B/op applies under these conditions and excludes cold compilation or new symbols.

```sh
python3 scripts/run-benchmarks.py --output results/local/MY_RUN
python3 scripts/render-results.py results/local/MY_RUN
```

For before/after comparison, copy the matching upstream control reports into MY_RUN/baseline/. The runner retains commands, environment, source/binary SHA-256, full BDN stdout and JSON/CSV/Markdown. render-results validates inputs and writes summary.json plus both README tables. verify-evidence checks copied files and published evidence.

A timing difference after extraction does not by itself establish an algorithm change: JIT, code layout, scheduling and background processes affect results. Compare intervals and raw samples. Verification establishes correctness and retained zero-allocation scenarios; tail latency was not measured separately.
