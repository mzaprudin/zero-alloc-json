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
| Standard_Deserialize_Class                         |       582.9 ns |      4.84 ns |      4.53 ns |     1.00 |    0.01 | 0.0070 |     120 B |        1.00 |
| JsonPopulator_Class_Reused_Interned                |       554.4 ns |      5.22 ns |      4.63 ns |     0.95 |    0.01 |      - |         - |        0.00 |
| JsonPopulator_Struct_Ref                           |       454.5 ns |      3.33 ns |      3.11 ns |     0.78 |    0.01 |      - |         - |        0.00 |
| Gate_Standard_Deserialize_List                     | 1,805,636.2 ns | 13,599.33 ns | 12,720.82 ns | 3,097.85 |   31.39 |      - |  141408 B |    1,178.40 |
| Gate_JsonPopulator_FastList_Reused_Interned        | 1,550,245.9 ns |  7,052.34 ns |  6,596.76 ns | 2,659.69 |   22.75 |      - |         - |        0.00 |
| Gate_JsonPopulator_FastList_Reused_Interned_Struct | 1,484,118.3 ns | 10,873.82 ns | 10,171.38 ns | 2,546.24 |   25.49 |      - |         - |        0.00 |
