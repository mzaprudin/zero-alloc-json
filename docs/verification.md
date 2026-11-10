# Verification record

[English](verification.md) · [Русский](verification.ru.md)

November 10, 2026: a separate clean git clone passed locked restore, Release build, all **56 tests** (0 failures/skips), and the standalone sample. Builds completed without warnings. The pinned upstream control passed all 126 tests.

The coexistence application referenced both libraries, verified JSON parsing/writing, 100 ordered JSON roundtrips through the public transport, the exported router, and 41 unique public types. Exported types and declared public members were compared with the upstream assembly; the agreed exception is internal PoolRental<T>.

All 22 BenchmarkDotNet cases were measured before and after, each with 5 raw warmup and 15 raw actual iterations. All previous B/op values are retained.

[Commands, reports and logs](../results/local/README.md) · [Provenance](provenance.md)

[Linux / Windows CI](https://github.com/mzaprudin/zero-alloc-json/actions/workflows/ci.yml)

[Independent acceptance audit](acceptance-audit.md)
