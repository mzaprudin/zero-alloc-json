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
| Standard_Serialize_Class     | 398.9 ns | 3.51 ns | 3.11 ns |  1.00 | 0.0113 |     192 B |        1.00 |
| Standard_Serialize_Writer    | 337.4 ns | 2.65 ns | 2.48 ns |  0.85 |      - |         - |        0.00 |
| JsonPushulator_Class         | 300.2 ns | 1.81 ns | 1.69 ns |  0.75 |      - |         - |        0.00 |
| JsonPushulator_Struct        | 259.8 ns | 0.52 ns | 0.46 ns |  0.65 |      - |         - |        0.00 |
| Manual_Utf8JsonWriter_Direct | 298.5 ns | 1.62 ns | 1.51 ns |  0.75 |      - |         - |        0.00 |
