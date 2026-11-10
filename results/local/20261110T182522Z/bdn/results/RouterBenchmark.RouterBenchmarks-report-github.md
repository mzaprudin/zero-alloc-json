```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.114
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-KOVDBU : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

IterationCount=15  IterationTime=500ms  LaunchCount=1  
WarmupCount=5  

```
| Method                      | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|------:|----------:|------------:|
| Route10kMessages_JsonReader | 81.53 ns | 0.426 ns | 0.333 ns |  1.00 |         - |          NA |
| Route10kMessages_Custom     | 47.89 ns | 0.260 ns | 0.217 ns |  0.59 |         - |          NA |
| Route10kMessages_Regex      | 93.77 ns | 0.664 ns | 0.554 ns |  1.15 |         - |          NA |
