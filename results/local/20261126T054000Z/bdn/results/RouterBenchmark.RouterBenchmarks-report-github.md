```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 9 5950X 0.55GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.115
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-KOVDBU : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

IterationCount=15  IterationTime=500ms  LaunchCount=1  
WarmupCount=5  

```
| Method                      | Mean      | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|---------------------------- |----------:|---------:|---------:|------:|----------:|------------:|
| Route10kMessages_JsonReader |  82.00 ns | 0.135 ns | 0.113 ns |  1.00 |         - |          NA |
| Route10kMessages_Custom     |  48.24 ns | 0.237 ns | 0.198 ns |  0.59 |         - |          NA |
| Route10kMessages_Regex      | 111.60 ns | 0.332 ns | 0.277 ns |  1.36 |         - |          NA |
