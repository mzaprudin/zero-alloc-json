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
| Standard_Deserialize_Class                         |       574.2 ns |      5.23 ns |      4.89 ns |     1.00 |    0.01 | 0.0068 |     120 B |        1.00 |
| JsonPopulator_Class_Reused_Interned                |       537.9 ns |      4.90 ns |      4.35 ns |     0.94 |    0.01 |      - |         - |        0.00 |
| JsonPopulator_Struct_Ref                           |       471.6 ns |      4.62 ns |      3.86 ns |     0.82 |    0.01 |      - |         - |        0.00 |
| Gate_Standard_Deserialize_List                     | 1,798,371.5 ns | 16,761.52 ns | 14,858.65 ns | 3,132.18 |   35.83 | 6.9444 |  141408 B |    1,178.40 |
| Gate_JsonPopulator_FastList_Reused_Interned        | 1,525,405.5 ns | 12,481.27 ns | 11,674.98 ns | 2,656.76 |   29.35 |      - |         - |        0.00 |
| Gate_JsonPopulator_FastList_Reused_Interned_Struct | 1,459,003.3 ns |  9,885.51 ns |  9,246.91 ns | 2,541.11 |   26.01 |      - |         - |        0.00 |
