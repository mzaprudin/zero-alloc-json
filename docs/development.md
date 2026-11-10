# Build and development

[English](development.md) · [Русский](development.ru.md)

Requires .NET 10 SDK and Python 3 for tooling. Package versions are pinned and packages.lock.json files are enforced by locked restore.

```sh
dotnet restore ZeroAlloc.Json.sln --locked-mode
dotnet build ZeroAlloc.Json.sln -c Release --no-restore
dotnet test tests/ZeroAlloc.Json.Tests -c Release --no-build
dotnet run -c Release --no-build --project samples/ZeroAlloc.Json.Demo
```

For clean verification, make a fresh git clone and run the same commands. CI runs all tests, the sample and every benchmark scenario with `--job dry` on Linux and Windows. Dry validates execution; published timing tables use full measured runs.
