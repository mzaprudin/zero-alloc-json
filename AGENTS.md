# Repository guidance

.NET 10; assembly `ZeroAlloc.Json`. Preserve imported namespaces, APIs, fixtures and algorithms.
Use spans, explicit loops and pooling on hot paths. Name nontrivial constants; document new types and members.
Keep cancellation propagation and memory ownership explicit. No third-party open-source license is granted; preserve NOTICE.

## Validation scope

Choose checks from the complete change, including earlier commits in a push and both paths of a rename. `scripts/ci_changes.py` is the executable routing policy. Mixed or unknown changes require full CI.

- Root Markdown, `docs/`, `results/`, README images under `assets/readme/`, and the presentation scripts `render-results.py` and `verify-evidence.py`: run `python scripts/verify-docs.py` and `python -m unittest discover -s scripts -p 'test_*.py' -v`. These check documentation links (including HTML image paths), SVG compatibility, Python syntax, generated tables and retained evidence. If rendering changes, regenerate from the published run and verify the output. Do not restore, build, run .NET tests/examples/benchmarks or repeat measurements solely to publish documentation.
- Library, tests, support, samples, benchmarks, fixtures, dependencies or build inputs: locked restore, Release build, every test, the sample and all benchmark cases with the Dry job, plus documentation/evidence checks and relevant tooling tests. Hosted CI checks Linux and Windows.
- Workflow, classification or documentation-validator logic: lint the workflow, run tooling tests and documentation/evidence checks locally; verify one full hosted matrix. Do not duplicate unchanged .NET checks locally solely for CI edits.

One Ubuntu documentation job runs on pushes to `main`, pull requests and manual dispatches. It selects the full Linux/Windows matrix for runtime, CI or unknown changes. Manual runs and unavailable Git history always select full CI. Feature branches are checked through pull requests; new commits cancel superseded runs for the same workflow and branch/PR. Keep workflow conditions, scripts and this policy aligned.
Re-run checks after relevant edits or failures rather than repeating successful checks without new evidence.

## Measurements and review

Real measurements use 5 warmups, 15 iterations, 500 ms, one launch; run sequentially.
Retain full raw evidence and source/binary fingerprints. Generate README tables from measurement JSON.
Require a separate read-only agent acceptance audit for runtime, build, CI logic or measurement-methodology changes and fix HIGH/BLOCKER findings. Ordinary documentation edits require the scoped checks above.
