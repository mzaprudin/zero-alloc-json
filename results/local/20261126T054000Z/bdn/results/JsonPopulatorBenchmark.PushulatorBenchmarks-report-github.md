```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.115
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-KOVDBU : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

IterationCount=15  IterationTime=500ms  LaunchCount=1  
WarmupCount=5  

```
| Method                       | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
| Standard_Serialize_Class     | 406.7 ns | 2.70 ns | 2.53 ns |  1.00 | 0.0114 |     192 B |        1.00 |
| Standard_Serialize_Writer    | 342.3 ns | 2.14 ns | 1.90 ns |  0.84 |      - |         - |        0.00 |
| JsonPushulator_Class         | 306.8 ns | 2.78 ns | 2.60 ns |  0.75 |      - |         - |        0.00 |
| JsonPushulator_Struct        | 262.7 ns | 1.51 ns | 1.34 ns |  0.65 |      - |         - |        0.00 |
| Manual_Utf8JsonWriter_Direct | 303.1 ns | 2.23 ns | 1.98 ns |  0.75 |      - |         - |        0.00 |
