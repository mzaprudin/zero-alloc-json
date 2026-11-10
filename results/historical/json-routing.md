```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3


```
| Method                      | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| Route10kMessages_JsonReader | 81.07 ns | 0.842 ns | 0.788 ns |  1.00 |    0.01 |         - |          NA |
| Route10kMessages_Custom     | 49.19 ns | 0.451 ns | 0.422 ns |  0.61 |    0.01 |         - |          NA |
| Route10kMessages_Regex      | 92.09 ns | 1.495 ns | 1.399 ns |  1.14 |    0.02 |         - |          NA |
