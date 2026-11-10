# Происхождение фикстур

[English](README.md) · [Русский](README.ru.md)

`trade.json` — синтетический контракт сделки из семи полей, основанный на исходном бенчмарке JsonPopulator из GapSonar. В нём нет данных аккаунта, персональных данных или ответов закрытого API. Все дополнительные сравнения разбора одинаковой модели используют этот UTF-8-файл и один класс `Trade`, включая `Symbol`.

Тесты и бенчмарки этого репозитория не подключаются к биржам и не требуют API-ключей. Транспортные проверки находятся в [pipelines-websocket](https://github.com/mzaprudin/pipelines-websocket).

`gate.futures.contracts.all.json` и `gate.futures.contracts.json` скопированы побайтно из исходных фикстур JsonPopulatorBenchmark в GapSonar. Полная фикстура содержит 592 записи публичных метаданных контрактов (1 000 081 байт), малая — 3 записи (5 168 байт). Исходный набор из шести population-сценариев использует полную фикстуру. Хеши приведены в [original-benchmarks-provenance.json](../docs/upstream/original-benchmarks-provenance.json).
