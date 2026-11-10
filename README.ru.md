# zero-alloc-json

[English](README.md) · [Русский](README.ru.md) · [CI](https://github.com/mzaprudin/zero-alloc-json/actions/workflows/ci.yml)

Разбор и запись JSON без аллокаций, переиспользуемые коллекции, интернирование строк и маршрутизация символов по UTF-8 на C# / .NET 10. Повторное использование объектов и буферов снижает нагрузку на GC и убирает выделение памяти на сообщение как источник непредсказуемых пауз при непрерывной обработке данных.

## Запуск

```sh
dotnet restore ZeroAlloc.Json.sln --locked-mode
dotnet build ZeroAlloc.Json.sln -c Release --no-restore
dotnet test tests/ZeroAlloc.Json.Tests -c Release --no-build
dotnet run -c Release --no-build --project samples/ZeroAlloc.Json.Demo
```

Требуется .NET 10 SDK. Перенесены все **56 тестов**. Пример проверяет все поля после разбора и побайтное совпадение записи со стандартным сериализатором.

## Локальные измерения новых сборок

AMD Ryzen 9 5950X · Ubuntu 24.04 · .NET 10.0.14 · BenchmarkDotNet 0.15.8. Release/x64; 5 прогревочных и 15 измерительных итераций по 500 мс, один запуск процесса на случай.

Кратность = время стандартного варианта / время метода; для DTO и списка используются свои стандартные варианты. Класс DTO включает Symbol, исходная структура — нет. Список содержит 592 контракта. Создание метаданных, рост коллекций и первое интернирование выполняются до измерения. Аллокации показаны фактическими байтами на операцию.

Кратность записи считается относительно стандартного переиспользуемого writer; строка нового byte[] сравнивается с собой.

<!-- LOCAL_RESULTS_START -->
### Разбор JSON

| Метод | Среднее | Погрешность (99,9% ДИ) | Память B/op | Ускорение |
|---|---:|---:|---:|---:|
| `Standard_Deserialize_Class` | 574.20 ns | 5.23 ns | 120 B | 1.00× |
| `JsonPopulator_Class_Reused_Interned` | 537.89 ns | 4.90 ns | 0 B | 1.07× |
| `JsonPopulator_Struct_Ref` | 471.62 ns | 4.62 ns | 0 B | 1.22× |
| `Gate_Standard_Deserialize_List` | 1.80 ms | 16.76 µs | 141408 B | 1.00× |
| `Gate_JsonPopulator_FastList_Reused_Interned` | 1.53 ms | 12.48 µs | 0 B | 1.18× |
| `Gate_JsonPopulator_FastList_Reused_Interned_Struct` | 1.46 ms | 9.89 µs | 0 B | 1.23× |

### Запись JSON

| Метод | Среднее | Погрешность (99,9% ДИ) | Память B/op | Ускорение |
|---|---:|---:|---:|---:|
| `Standard_Serialize_Class` | 392.77 ns | 2.37 ns | 192 B | 1.00× |
| `Standard_Serialize_Writer` | 364.99 ns | 2.31 ns | 0 B | 1.00× |
| `JsonPushulator_Class` | 295.35 ns | 1.30 ns | 0 B | 1.24× |
| `JsonPushulator_Struct` | 273.27 ns | 1.77 ns | 0 B | 1.34× |
| `Manual_Utf8JsonWriter_Direct` | 302.87 ns | 1.50 ns | 0 B | 1.21× |

### Маршрутизация UTF-8

| Метод | Среднее | Погрешность (99,9% ДИ) | Память B/op | Ускорение |
|---|---:|---:|---:|---:|
| `Route10kMessages_JsonReader` | 81.53 ns | 0.43 ns | 0 B | 1.00× |
| `Route10kMessages_Custom` | 47.89 ns | 0.26 ns | 0 B | 1.70× |
| `Route10kMessages_Regex` | 93.77 ns | 0.66 ns | 0 B | 0.87× |

### Разбор одинаковой модели

| Метод | Среднее | Погрешность (99,9% ДИ) | Память B/op | Ускорение |
|---|---:|---:|---:|---:|
| `SystemTextJson` | 521.36 ns | 7.42 ns | 120 B | 1.00× |
| `SystemTextJsonGenerated` | 563.29 ns | 3.87 ns | 120 B | 0.93× |
| `PopulatorNewObject` | 493.52 ns | 5.49 ns | 120 B | 1.06× |
| `PopulatorReusedObject` | 432.74 ns | 3.68 ns | 40 B | 1.20× |
| `PopulatorReusedInterned` | 430.47 ns | 5.20 ns | 0 B | 1.21× |

### Запись одинаковой модели

| Метод | Среднее | Погрешность (99,9% ДИ) | Память B/op | Ускорение |
|---|---:|---:|---:|---:|
| `SystemTextJson` | 357.00 ns | 2.50 ns | 0 B | 1.00× |
| `SystemTextJsonGenerated` | 268.14 ns | 1.38 ns | 0 B | 1.33× |
| `PushulatorReusedWriter` | 308.00 ns | 2.45 ns | 0 B | 1.16× |

<!-- LOCAL_RESULTS_END -->

[Полные отчёты, исходные значения и сравнение до/после](results/local/README.md) · [Методика](docs/benchmark-methodology.ru.md)

```sh
python3 scripts/run-benchmarks.py
python3 scripts/render-results.py results/local/RUN_DIRECTORY
```

## Устройство и проверка

[Владение памятью и ограничения](docs/architecture.ru.md) · [Сборка и CI](docs/development.ru.md) · [Проверки](docs/verification.ru.md) · [Происхождение](docs/provenance.ru.md)

Самостоятельная сборка `ZeroAlloc.Json` сохраняет пространства имён GapSonar. Зависимости от [pipelines-websocket](https://github.com/mzaprudin/pipelines-websocket) нет; обе библиотеки можно подключить одновременно.

Опубликовано для портфолио без открытой лицензии. См. [NOTICE](NOTICE).
