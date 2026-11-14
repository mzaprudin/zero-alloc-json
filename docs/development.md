# Build and development

[English](development.md) · [Русский](development.ru.md)

Python 3 runs documentation and CI-tooling checks. C# and build changes also require the .NET 10 SDK. Package versions are pinned and packages.lock.json files are enforced by locked restore.

## Documentation and README assets

For root Markdown, `docs/`, `results/`, README images and presentation scripts:

```sh
python scripts/verify-docs.py
python -m unittest discover -s scripts -p 'test_*.py' -v
```

These checks validate Markdown and HTML links, local image paths, SVG syntax and dependencies, Python syntax, generated benchmark tables and retained evidence. Archived reports stay byte-identical. When changing table generation, regenerate from the [published run](../results/local/published-run.json) and verify the output. Documentation-only publication does not require a .NET build, tests, sample or new measurements.

## Code and build inputs

For library code, tests, support, samples, benchmarks, fixtures, dependencies and build inputs:

```sh
dotnet restore ZeroAlloc.Json.sln --locked-mode
dotnet build ZeroAlloc.Json.sln -c Release --no-restore
dotnet test tests/ZeroAlloc.Json.Tests -c Release --no-build
dotnet run -c Release --no-build --project samples/ZeroAlloc.Json.Demo
dotnet run -c Release --no-build --project benchmarks/ZeroAlloc.Json.Benchmarks -- --filter '*' --job dry
python scripts/verify-docs.py
```

For clean verification, make a fresh git clone and run the same commands. Run relevant tooling tests as well. CI runs all tests, the sample and every benchmark scenario with `--job dry` on Linux and Windows for these changes. Dry validates execution; published timing tables use full measured runs.

## CI scope and scheduling

Automatic CI runs on pushes to `main` and on pull requests. Feature branches use pull-request checks, avoiding duplicate push/PR runs. New commits cancel superseded runs for the same workflow and branch/PR.

One Ubuntu job classifies the complete push or PR diff and checks documentation/evidence. The Linux/Windows runtime matrix runs for code, CI configuration and unknown inputs. Both paths of renamed files are considered. Manual dispatches, incomplete event data and unavailable history select full CI.

For workflow, classification or documentation-validator changes, lint the workflow and run the Python tests and documentation/evidence checks locally, then verify one full hosted matrix. Unchanged .NET checks do not need a duplicate local run solely for CI edits. The routing policy is in [ci_changes.py](../scripts/ci_changes.py); local requirements are also recorded in [AGENTS.md](../AGENTS.md).
