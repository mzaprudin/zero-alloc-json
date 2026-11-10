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

AMD Ryzen 9 5950X · Ubuntu 24.04 · .NET 10.0.14 · BenchmarkDotNet 0.15.8. Release/x64; 5 прогревочных и 15 измерительных итераций по 500 мс, один процесс.

Кратность = время стандартного варианта / время метода; для DTO и списка используются свои стандартные варианты. Класс DTO включает Symbol, исходная структура — нет. Список содержит 592 контракта. Создание метаданных, рост коллекций и первое интернирование выполняются до измерения. Аллокации показаны фактическими байтами на операцию.

<!-- LOCAL_RESULTS_START -->
Measurements are being prepared.
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
