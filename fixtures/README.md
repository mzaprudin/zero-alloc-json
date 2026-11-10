# Fixture provenance

`trade.json` is a deterministic synthetic seven-field trade contract adapted
from the original GapSonar JsonPopulator benchmark. It contains no account,
order, personal, or authenticated API data. All JSON baseline methods consume
this same UTF-8 file and the same `Trade` class, including `Symbol`.

WebSocket tests also generate deterministic binary payloads at frame-length
boundaries. No test or benchmark connects to an exchange or needs an API key.

`gate.futures.contracts.all.json` and `gate.futures.contracts.json` are copied
byte-for-byte from GapSonar's original JsonPopulatorBenchmark fixtures. The
complete fixture contains 592 public contract-metadata records (1,000,081 bytes);
the small fixture contains 3 records (5,168 bytes). The original six-method
population suite uses the complete fixture. File hashes are recorded in
`docs/original-benchmarks-provenance.json`.
