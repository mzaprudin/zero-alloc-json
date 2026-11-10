```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.114
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-KOVDBU : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

IterationCount=15  IterationTime=500ms  LaunchCount=1  
WarmupCount=5  

```
| Method                  | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------------ |---------:|--------:|--------:|------:|----------:|------------:|
| SystemTextJson          | 339.2 ns | 2.52 ns | 2.36 ns |  1.00 |         - |          NA |
| SystemTextJsonGenerated | 279.5 ns | 2.36 ns | 2.09 ns |  0.82 |         - |          NA |
| PushulatorReusedWriter  | 298.0 ns | 2.08 ns | 1.95 ns |  0.88 |         - |          NA |
