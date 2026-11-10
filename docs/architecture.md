# Architecture and memory ownership

[English](architecture.md) · [Русский](architecture.ru.md)

`JsonPopulator<T>` caches reflection metadata, UTF-8 member lookup and compiled setters per closed type. Inputs are UTF-8 spans or sequences; destinations are caller-owned classes, structs by reference or reusable FastList collections. `[JsonPosition]` supports positional arrays. Fixed strings avoid managed string allocation; optional interning reuses previously seen strings.

Initialization compiles expressions and allocates metadata. Steady-state 0 B/op requires warm metadata, sufficient collection capacity, reusable destinations and warmed/bounded interned values. Missing fields intentionally retain their old values. Collection growth and previously unseen symbols allocate. The global compatibility interner retains symbols for the process lifetime; a session-owned interner gives the caller a clearer lifetime.

Quoted-number parsing preserves permissive legacy behavior, including accepting a numeric prefix or returning zero after some failed conversions. Unsupported mappings may be skipped. Exercise representative fixtures for each model. This API does not provide all validation/converter semantics of JsonSerializer.

`JsonPushulator` compiles writes and accepts an existing Utf8JsonWriter. The caller owns output capacity, buffer/writer reset, flushing, disposal and synchronization. Never concurrently populate a reused destination or reset a writer while another operation uses it.

`RouterBenchmark.CustomMessageRouter`, `JsonMessageRouter`, `RegexMessageRouter`, `SymbolLookup` and `IMessageRouter<T>` now reside in the library with their original namespaces. The fast router searches a compact ASCII `"symbol":"` marker; escaped values and arbitrary JSON formatting require a general parser. Multi-segment input in the original custom/regex routers copies to an array. Routing benchmarks use single-segment compact messages and FrozenDictionary alternate lookup.

PoolRental<T> is internal. Its implementation is privately copied into the transport assembly, allowing both libraries to coexist without public type collisions.
