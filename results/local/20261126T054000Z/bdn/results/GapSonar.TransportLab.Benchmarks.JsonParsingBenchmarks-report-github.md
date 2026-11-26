```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.115
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-KOVDBU : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

IterationCount=15  IterationTime=500ms  LaunchCount=1  
WarmupCount=5  

```
| Method                  | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------ |---------:|--------:|--------:|------:|-------:|----------:|------------:|
| SystemTextJson          | 530.6 ns | 4.04 ns | 3.58 ns |  1.00 | 0.0064 |     120 B |        1.00 |
| SystemTextJsonGenerated | 503.3 ns | 6.68 ns | 6.25 ns |  0.95 | 0.0071 |     120 B |        1.00 |
| PopulatorNewObject      | 476.5 ns | 3.00 ns | 2.51 ns |  0.90 | 0.0068 |     120 B |        1.00 |
| PopulatorReusedObject   | 432.7 ns | 3.17 ns | 2.65 ns |  0.82 | 0.0017 |      40 B |        0.33 |
| PopulatorReusedInterned | 431.6 ns | 3.60 ns | 3.01 ns |  0.81 |      - |         - |        0.00 |
