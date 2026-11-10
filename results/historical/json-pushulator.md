```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3


```
| Method                       | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
| Standard_Serialize_Class     | 369.0 ns | 1.79 ns | 1.49 ns |  1.00 | 0.0114 |     192 B |        1.00 |
| Standard_Serialize_Writer    | 329.7 ns | 1.01 ns | 0.95 ns |  0.89 |      - |         - |        0.00 |
| JsonPushulator_Class         | 281.6 ns | 1.13 ns | 1.00 ns |  0.76 |      - |         - |        0.00 |
| JsonPushulator_Struct        | 251.7 ns | 0.81 ns | 0.75 ns |  0.68 |      - |         - |        0.00 |
| Manual_Utf8JsonWriter_Direct | 285.3 ns | 1.34 ns | 1.19 ns |  0.77 |      - |         - |        0.00 |
