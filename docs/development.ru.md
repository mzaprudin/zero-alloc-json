# Сборка и разработка

[English](development.md) · [Русский](development.ru.md)

Требуются .NET 10 SDK и Python 3 для скриптов. Все пакеты имеют фиксированные версии; packages.lock.json проверяются в locked restore.

```sh
dotnet restore ZeroAlloc.Json.sln --locked-mode
dotnet build ZeroAlloc.Json.sln -c Release --no-restore
dotnet test tests/ZeroAlloc.Json.Tests -c Release --no-build
dotnet run -c Release --no-build --project samples/ZeroAlloc.Json.Demo
```

Для чистой проверки создайте новый git clone и выполните те же команды. CI запускает все тесты, пример и каждый benchmark-сценарий с `--job dry` на Linux и Windows. Dry проверяет работоспособность; для таблиц используются реальные длительные замеры.
