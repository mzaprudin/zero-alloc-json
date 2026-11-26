```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.115
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-KOVDBU : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

IterationCount=15  IterationTime=500ms  LaunchCount=1  
WarmupCount=5  

```
| Method                  | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------------ |---------:|--------:|--------:|------:|----------:|------------:|
| SystemTextJson          | 347.1 ns | 1.83 ns | 1.52 ns |  1.00 |         - |          NA |
| SystemTextJsonGenerated | 268.8 ns | 2.54 ns | 2.37 ns |  0.77 |         - |          NA |
| PushulatorReusedWriter  | 310.2 ns | 1.63 ns | 1.44 ns |  0.89 |         - |          NA |
