# Результаты проверки

[English](verification.md) · [Русский](verification.ru.md)

10 ноября 2026 года: из отдельного чистого git clone выполнены locked restore, Release build, все **56 тестов** (0 ошибок, 0 пропусков) и самостоятельный пример. Сборки завершились без предупреждений. На исходной ревизии прошли все 126 тестов.

Совместное приложение подключило обе библиотеки, проверило разбор и запись JSON, 100 упорядоченных JSON-обменов через публичный транспорт, экспортированный router и 41 уникальный публичный тип. Состав публичных типов и объявленных членов сравнен с исходной сборкой; исключение — согласованный internal PoolRental<T>.

Измерены все 22 случая BenchmarkDotNet до и после; в каждом есть 5 raw warmup и 15 raw actual итераций. Все прежние значения B/op сохранены.

[Команды, отчёты и логи](../results/local/README.md) · [Происхождение](provenance.ru.md)

[Linux / Windows CI](https://github.com/mzaprudin/zero-alloc-json/actions/workflows/ci.yml)

[Independent acceptance audit](acceptance-audit.ru.md)
