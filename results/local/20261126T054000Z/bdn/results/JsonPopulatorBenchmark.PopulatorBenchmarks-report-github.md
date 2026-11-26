```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.115
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-KOVDBU : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

IterationCount=15  IterationTime=500ms  LaunchCount=1  
WarmupCount=5  

```
| Method                                             | Mean           | Error        | StdDev       | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------------:|-------------:|-------------:|---------:|--------:|-------:|----------:|------------:|
| Standard_Deserialize_Class                         |       577.2 ns |      5.51 ns |      4.88 ns |     1.00 |    0.01 | 0.0070 |     120 B |        1.00 |
| JsonPopulator_Class_Reused_Interned                |       539.0 ns |      3.12 ns |      2.77 ns |     0.93 |    0.01 |      - |         - |        0.00 |
| JsonPopulator_Struct_Ref                           |       458.8 ns |      5.16 ns |      4.57 ns |     0.79 |    0.01 |      - |         - |        0.00 |
| Gate_Standard_Deserialize_List                     | 1,797,177.3 ns | 10,871.71 ns | 10,169.41 ns | 3,113.74 |   30.68 | 6.9444 |  141408 B |    1,178.40 |
| Gate_JsonPopulator_FastList_Reused_Interned        | 1,510,224.0 ns |  9,467.68 ns |  8,392.85 ns | 2,616.57 |   25.62 |      - |         - |        0.00 |
| Gate_JsonPopulator_FastList_Reused_Interned_Struct | 1,450,685.0 ns | 13,696.18 ns | 12,141.31 ns | 2,513.41 |   28.92 |      - |         - |        0.00 |
