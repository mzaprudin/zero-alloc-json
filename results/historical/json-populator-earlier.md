```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3


```
| Method                                             | Mean           | Error        | StdDev      | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------------:|-------------:|------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Standard_Deserialize_Class                         |       533.7 ns |      1.49 ns |     1.39 ns |     1.00 |    0.00 | 0.0067 |      - |     120 B |        1.00 |
| Standard_Deserialize_Struct                        |       499.7 ns |      0.90 ns |     0.80 ns |     0.94 |    0.00 | 0.0038 |      - |      72 B |        0.60 |
| JsonPopulator_Class_Reused_Interned                |       477.6 ns |      2.10 ns |     1.86 ns |     0.89 |    0.00 |      - |      - |         - |        0.00 |
| JsonPopulator_Class_Reused_NoInterner              |       473.0 ns |      3.33 ns |     3.12 ns |     0.89 |    0.01 | 0.0024 |      - |      40 B |        0.33 |
| JsonPopulator_Struct_Ref                           |       435.3 ns |      0.94 ns |     0.78 ns |     0.82 |    0.00 |      - |      - |         - |        0.00 |
| Gate_Standard_Deserialize_List                     | 1,673,316.4 ns |  9,422.32 ns | 8,813.65 ns | 3,135.17 |   17.84 | 7.8125 | 1.9531 |  141408 B |    1,178.40 |
| Gate_JsonPopulator_FastList_Reused_Interned        | 1,433,149.5 ns | 10,580.20 ns | 9,896.73 ns | 2,685.18 |   19.19 |      - |      - |         - |        0.00 |
| Gate_JsonPopulator_FastList_Reused_Interned_Struct | 1,387,964.1 ns |  2,660.33 ns | 2,358.31 ns | 2,600.52 |    7.83 |      - |      - |         - |        0.00 |
