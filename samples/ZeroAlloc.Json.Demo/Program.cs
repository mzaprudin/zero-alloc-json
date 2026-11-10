using System.Buffers;
using System.Text.Json;
using GapSonar.Core.Shared;
using GapSonar.Core.Shared.Infrastructure.Serialization;
using GapSonar.TransportLab.Support;

const int OUTPUT_CAPACITY_BYTES = 1_024;
var fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fixtures", "trade.json"));
var expected = JsonSerializer.Deserialize(fixture, TradeJsonContext.Default.Trade)!;
var trade = new Trade();
var interner = new SimpleStringInterner();
JsonPopulator.Populate(fixture, trade, interner.Intern);
if (JsonSerializer.Serialize(trade) != JsonSerializer.Serialize(expected))
    throw new InvalidDataException("Parsed trade differs from the framework baseline.");
var output = new ArrayBufferWriter<byte>(OUTPUT_CAPACITY_BYTES);
using var writer = new Utf8JsonWriter(output);
JsonPushulator.Push(trade, writer);
writer.Flush();
if (!output.WrittenSpan.SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(expected, TradeJsonContext.Default.Trade)))
    throw new InvalidDataException("Written trade differs from the framework baseline.");
Console.WriteLine($"PASS: parsed and wrote {output.WrittenCount} JSON bytes; all fields verified.");
