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
| SystemTextJson          | 357.0 ns | 2.50 ns | 2.22 ns |  1.00 |         - |          NA |
| SystemTextJsonGenerated | 268.1 ns | 1.38 ns | 1.23 ns |  0.75 |         - |          NA |
| PushulatorReusedWriter  | 308.0 ns | 2.45 ns | 2.17 ns |  0.86 |         - |          NA |
