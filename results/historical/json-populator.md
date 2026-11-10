```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3


```
| Method                                             | Mean           | Error       | StdDev      | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------------:|------------:|------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Standard_Deserialize_Class                         |       536.6 ns |     1.39 ns |     1.23 ns |     1.00 |    0.00 | 0.0067 |      - |     120 B |        1.00 |
| JsonPopulator_Class_Reused_Interned                |       510.2 ns |     1.02 ns |     0.85 ns |     0.95 |    0.00 |      - |      - |         - |        0.00 |
| JsonPopulator_Struct_Ref                           |       434.6 ns |     0.52 ns |     0.49 ns |     0.81 |    0.00 |      - |      - |         - |        0.00 |
| Gate_Standard_Deserialize_List                     | 1,672,420.8 ns | 6,711.02 ns | 5,949.15 ns | 3,116.69 |   12.74 | 7.8125 | 1.9531 |  141408 B |    1,178.40 |
| Gate_JsonPopulator_FastList_Reused_Interned        | 1,461,313.8 ns | 4,972.80 ns | 4,651.56 ns | 2,723.27 |   10.33 |      - |      - |         - |        0.00 |
| Gate_JsonPopulator_FastList_Reused_Interned_Struct | 1,418,176.2 ns | 3,595.01 ns | 3,362.78 ns | 2,642.88 |    8.42 |      - |      - |         - |        0.00 |
