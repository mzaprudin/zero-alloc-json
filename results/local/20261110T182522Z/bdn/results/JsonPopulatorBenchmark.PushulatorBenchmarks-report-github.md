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
| Standard_Serialize_Class     | 392.8 ns | 2.37 ns | 2.22 ns |  1.00 | 0.0111 |     192 B |        1.00 |
| Standard_Serialize_Writer    | 365.0 ns | 2.31 ns | 2.16 ns |  0.93 |      - |         - |        0.00 |
| JsonPushulator_Class         | 295.3 ns | 1.30 ns | 1.22 ns |  0.75 |      - |         - |        0.00 |
| JsonPushulator_Struct        | 273.3 ns | 1.77 ns | 1.57 ns |  0.70 |      - |         - |        0.00 |
| Manual_Utf8JsonWriter_Direct | 302.9 ns | 1.50 ns | 1.33 ns |  0.77 |      - |         - |        0.00 |
