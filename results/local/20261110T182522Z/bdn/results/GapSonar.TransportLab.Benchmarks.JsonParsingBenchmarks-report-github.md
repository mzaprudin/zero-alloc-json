```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.114
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-KOVDBU : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

IterationCount=15  IterationTime=500ms  LaunchCount=1  
WarmupCount=5  

```
| Method                  | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| SystemTextJson          | 521.4 ns | 7.42 ns | 6.94 ns |  1.00 |    0.02 | 0.0063 |     120 B |        1.00 |
| SystemTextJsonGenerated | 563.3 ns | 3.87 ns | 3.43 ns |  1.08 |    0.02 | 0.0068 |     120 B |        1.00 |
| PopulatorNewObject      | 493.5 ns | 5.49 ns | 5.13 ns |  0.95 |    0.02 | 0.0070 |     120 B |        1.00 |
| PopulatorReusedObject   | 432.7 ns | 3.68 ns | 3.44 ns |  0.83 |    0.01 | 0.0017 |      40 B |        0.33 |
| PopulatorReusedInterned | 430.5 ns | 5.20 ns | 4.87 ns |  0.83 |    0.01 |      - |         - |        0.00 |
