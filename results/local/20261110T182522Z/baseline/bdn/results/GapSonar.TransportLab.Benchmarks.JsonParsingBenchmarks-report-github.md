```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.114
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-KOVDBU : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

IterationCount=15  IterationTime=500ms  LaunchCount=1  
WarmupCount=5  

```
| Method                  | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------ |---------:|--------:|--------:|------:|-------:|----------:|------------:|
| SystemTextJson          | 521.6 ns | 5.93 ns | 5.55 ns |  1.00 | 0.0064 |     120 B |        1.00 |
| SystemTextJsonGenerated | 497.1 ns | 5.74 ns | 5.37 ns |  0.95 | 0.0069 |     120 B |        1.00 |
| PopulatorNewObject      | 475.3 ns | 5.55 ns | 5.20 ns |  0.91 | 0.0068 |     120 B |        1.00 |
| PopulatorReusedObject   | 422.3 ns | 2.72 ns | 2.41 ns |  0.81 | 0.0017 |      40 B |        0.33 |
| PopulatorReusedInterned | 440.1 ns | 4.70 ns | 4.39 ns |  0.84 |      - |         - |        0.00 |
