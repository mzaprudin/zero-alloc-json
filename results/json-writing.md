```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.114
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-CKHFMU : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

IterationCount=15  IterationTime=500ms  LaunchCount=2  
WarmupCount=5  

```
| Method                  | Mean     | Error   | StdDev  | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------ |---------:|--------:|--------:|------:|--------:|----------:|------------:|
| SystemTextJson          | 349.7 ns | 0.81 ns | 1.19 ns |  1.00 |    0.00 |         - |          NA |
| SystemTextJsonGenerated | 274.1 ns | 3.96 ns | 5.68 ns |  0.78 |    0.02 |         - |          NA |
| PushulatorReusedWriter  | 318.0 ns | 6.18 ns | 8.87 ns |  0.91 |    0.03 |         - |          NA |
