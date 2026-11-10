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
| Route10kMessages_JsonReader | 81.54 ns | 0.465 ns | 0.435 ns |  1.00 |         - |          NA |
| Route10kMessages_Custom     | 52.29 ns | 0.546 ns | 0.510 ns |  0.64 |         - |          NA |
| Route10kMessages_Regex      | 90.44 ns | 0.404 ns | 0.378 ns |  1.11 |         - |          NA |
