```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.114
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-CKHFMU : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

IterationCount=15  IterationTime=500ms  LaunchCount=2  
WarmupCount=5  

```
| Method                  | Mean     | Error   | StdDev   | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |---------:|--------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| SystemTextJson          | 515.7 ns | 9.33 ns | 13.97 ns | 514.6 ns |  1.00 |    0.04 | 0.0062 |     120 B |        1.00 |
| SystemTextJsonGenerated | 503.8 ns | 1.34 ns |  1.97 ns | 503.9 ns |  0.98 |    0.03 | 0.0070 |     120 B |        1.00 |
| PopulatorNewObject      | 481.1 ns | 5.90 ns |  8.28 ns | 477.4 ns |  0.93 |    0.03 | 0.0069 |     120 B |        1.00 |
| PopulatorReusedObject   | 434.4 ns | 9.87 ns | 14.47 ns | 424.7 ns |  0.84 |    0.04 | 0.0018 |      40 B |        0.33 |
| PopulatorReusedInterned | 425.7 ns | 2.07 ns |  3.10 ns | 424.7 ns |  0.83 |    0.02 |      - |         - |        0.00 |
