# Local split comparison / Локальная проверка разделения

Run `20261110T182522Z` · UTC November 10, 2026. Upstream control: `02f7aefda0933e9a719616ec72bf6f11fc5dd9a8`. Split source: `1ce339453e4c2d8852b8fb0aa0955f0fa8bcc16b`.

22 BenchmarkDotNet cases before and after; five warmups and fifteen actual iterations at a 500 ms target, one launch. Measurements ran sequentially on this machine.

All per-operation allocation values match before/after: **True**. Observed after/before mean ratios range **0.916–1.133**. These are individual local observations with JIT/layout/scheduling effects; raw confidence intervals and all samples are retained.

Все 22 случая измерены до и после. Аллокации на операцию совпадают; отношение среднего времени после/до — 0.916–1.133. Это наблюдения одного локального сравнения: учитывайте интервалы, JIT, размещение кода и планировщик.

- [Complete after reports](20261110T182522Z/bdn/results/) and [raw output](20261110T182522Z/benchmark.log)
- [Before reports](20261110T182522Z/baseline/bdn/results/) and [raw output](20261110T182522Z/baseline/benchmark.log)
- [Comparison JSON](20261110T182522Z/summary.json)
- [Commands](20261110T182522Z/commands.json), [environment](20261110T182522Z/environment.json), [source hashes](20261110T182522Z/source-snapshot.json), [binary hashes](20261110T182522Z/binaries.json)
- [Artifact hashes](20261110T182522Z/artifacts.sha256.json), [baseline hashes](20261110T182522Z/baseline/artifacts.sha256.json)
- [Exact clean-check commands](20261110T182522Z/verification/clean-commands.json) and [verification logs](20261110T182522Z/verification/)
- [Coexistence source snapshot](20261110T182522Z/verification/coexistence/) and [passed public API/integrity check](20261110T182522Z/verification/coexistence.log)

| Case | Before | After | After/before | B/op before → after |
|---|---:|---:|---:|---:|
| `JsonParsingBenchmarks.SystemTextJson`  | 521.60 ns | 521.36 ns | 1.000× | 120 → 120 |
| `JsonParsingBenchmarks.SystemTextJsonGenerated`  | 497.14 ns | 563.29 ns | 1.133× | 120 → 120 |
| `JsonParsingBenchmarks.PopulatorNewObject`  | 475.28 ns | 493.52 ns | 1.038× | 120 → 120 |
| `JsonParsingBenchmarks.PopulatorReusedObject`  | 422.31 ns | 432.74 ns | 1.025× | 40 → 40 |
| `JsonParsingBenchmarks.PopulatorReusedInterned`  | 440.08 ns | 430.47 ns | 0.978× | 0 → 0 |
| `JsonWritingBenchmarks.SystemTextJson`  | 339.21 ns | 357.00 ns | 1.052× | 0 → 0 |
| `JsonWritingBenchmarks.SystemTextJsonGenerated`  | 279.54 ns | 268.14 ns | 0.959× | 0 → 0 |
| `JsonWritingBenchmarks.PushulatorReusedWriter`  | 298.03 ns | 308.00 ns | 1.033× | 0 → 0 |
| `PopulatorBenchmarks.Standard_Deserialize_Class`  | 587.94 ns | 574.20 ns | 0.977× | 120 → 120 |
| `PopulatorBenchmarks.JsonPopulator_Class_Reused_Interned`  | 552.21 ns | 537.89 ns | 0.974× | 0 → 0 |
| `PopulatorBenchmarks.JsonPopulator_Struct_Ref`  | 456.59 ns | 471.62 ns | 1.033× | 0 → 0 |
| `PopulatorBenchmarks.Gate_Standard_Deserialize_List`  | 1803583.23 ns | 1798371.55 ns | 0.997× | 141408 → 141408 |
| `PopulatorBenchmarks.Gate_JsonPopulator_FastList_Reused_Interned`  | 1517890.52 ns | 1525405.46 ns | 1.005× | 0 → 0 |
| `PopulatorBenchmarks.Gate_JsonPopulator_FastList_Reused_Interned_Struct`  | 1482517.94 ns | 1459003.25 ns | 0.984× | 0 → 0 |
| `PushulatorBenchmarks.Standard_Serialize_Class`  | 401.44 ns | 392.77 ns | 0.978× | 192 → 192 |
| `PushulatorBenchmarks.Standard_Serialize_Writer`  | 340.35 ns | 364.99 ns | 1.072× | 0 → 0 |
| `PushulatorBenchmarks.JsonPushulator_Class`  | 296.87 ns | 295.35 ns | 0.995× | 0 → 0 |
| `PushulatorBenchmarks.JsonPushulator_Struct`  | 266.29 ns | 273.27 ns | 1.026× | 0 → 0 |
| `PushulatorBenchmarks.Manual_Utf8JsonWriter_Direct`  | 301.36 ns | 302.87 ns | 1.005× | 0 → 0 |
| `RouterBenchmarks.Route10kMessages_JsonReader`  | 81.54 ns | 81.53 ns | 1.000× | 0 → 0 |
| `RouterBenchmarks.Route10kMessages_Custom`  | 52.29 ns | 47.89 ns | 0.916× | 0 → 0 |
| `RouterBenchmarks.Route10kMessages_Regex`  | 90.44 ns | 93.77 ns | 1.037× | 0 → 0 |
