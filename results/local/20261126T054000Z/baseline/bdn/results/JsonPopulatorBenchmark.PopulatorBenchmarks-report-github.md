```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.114
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-KOVDBU : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

IterationCount=15  IterationTime=500ms  LaunchCount=1  
WarmupCount=5  

```
| Method                                             | Mean           | Error        | StdDev       | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------------:|-------------:|-------------:|---------:|--------:|-------:|----------:|------------:|
| Standard_Deserialize_Class                         |       587.9 ns |      6.12 ns |      5.73 ns |     1.00 |    0.01 | 0.0072 |     120 B |        1.00 |
| JsonPopulator_Class_Reused_Interned                |       552.2 ns |      3.12 ns |      2.77 ns |     0.94 |    0.01 |      - |         - |        0.00 |
| JsonPopulator_Struct_Ref                           |       456.6 ns |      3.16 ns |      2.80 ns |     0.78 |    0.01 |      - |         - |        0.00 |
| Gate_Standard_Deserialize_List                     | 1,803,583.2 ns | 19,206.53 ns | 17,026.09 ns | 3,067.92 |   40.16 |      - |  141408 B |    1,178.40 |
| Gate_JsonPopulator_FastList_Reused_Interned        | 1,517,890.5 ns | 15,716.26 ns | 14,700.99 ns | 2,581.95 |   34.27 |      - |         - |        0.00 |
| Gate_JsonPopulator_FastList_Reused_Interned_Struct | 1,482,517.9 ns | 15,929.77 ns | 14,900.72 ns | 2,521.78 |   34.10 |      - |         - |        0.00 |
