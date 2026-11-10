```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.114
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-KOVDBU : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

IterationCount=15  IterationTime=500ms  LaunchCount=1  
WarmupCount=5  

```
| Method                       | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
| Standard_Serialize_Class     | 401.4 ns | 4.55 ns | 4.03 ns |  1.00 | 0.0114 |     192 B |        1.00 |
| Standard_Serialize_Writer    | 340.4 ns | 2.26 ns | 2.01 ns |  0.85 |      - |         - |        0.00 |
| JsonPushulator_Class         | 296.9 ns | 2.78 ns | 2.60 ns |  0.74 |      - |         - |        0.00 |
| JsonPushulator_Struct        | 266.3 ns | 1.94 ns | 1.81 ns |  0.66 |      - |         - |        0.00 |
| Manual_Utf8JsonWriter_Direct | 301.4 ns | 2.32 ns | 2.17 ns |  0.75 |      - |         - |        0.00 |
