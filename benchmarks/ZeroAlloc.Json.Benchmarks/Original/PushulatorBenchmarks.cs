using System.Buffers;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using GapSonar.Core.Shared.Infrastructure.Serialization;

namespace JsonPopulatorBenchmark;

/// <summary>Original GapSonar fresh-buffer, reused-writer, class, struct and manual serialization workloads.</summary>
[MemoryDiagnoser]
public class PushulatorBenchmarks
{
    /// <summary>Original reusable output capacity.</summary>
    private const int WRITER_CAPACITY_BYTES = 4_096;
    private TradeDto _dto = null!;
    private TradeStruct _struct;
    private ArrayBufferWriter<byte> _bufferWriter = new(WRITER_CAPACITY_BYTES);
    private Utf8JsonWriter _utf8JsonWriter = null!;

    /// <summary>Initializes and warms the original benchmark data outside measurements.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _dto = new TradeDto
        {
            Id = 123456789,
            Price = 12345.6789,
            Amount = 0.00123456m,
            Symbol = "BTCUSDT",
            Timestamp = 1678900000000,
            IsBuyerMaker = true,
            CreatedAt = DateTime.UtcNow
        };
        _struct = new TradeStruct
        {
            Id = 123456789,
            Price = 12345.6789,
            Amount = 0.00123456m,
            Timestamp = 1678900000000,
            IsBuyerMaker = true,
            CreatedAt = DateTime.UtcNow
        };

        _utf8JsonWriter = new Utf8JsonWriter(_bufferWriter);

        // Warmup
        _bufferWriter.Clear();
        _utf8JsonWriter.Reset();
        JsonPushulator.Push(_dto, _utf8JsonWriter);
        _utf8JsonWriter.Flush();
        if (!JsonSerializer.SerializeToUtf8Bytes(_dto).AsSpan().SequenceEqual(_bufferWriter.WrittenSpan))
            throw new InvalidDataException("Original class serialization differs from the framework baseline.");
        JsonPushulator_Struct();
        if (!JsonSerializer.SerializeToUtf8Bytes(_struct).AsSpan().SequenceEqual(_bufferWriter.WrittenSpan))
            throw new InvalidDataException("Original struct serialization differs from the framework baseline.");
    }

    /// <summary>Serializes the original class to a fresh UTF-8 byte array.</summary>
    [Benchmark(Baseline = true)]
    public byte[] Standard_Serialize_Class()
    {
        return JsonSerializer.SerializeToUtf8Bytes(_dto);
    }

    /// <summary>Serializes the original class to the reused writer.</summary>
    [Benchmark]
    public void Standard_Serialize_Writer()
    {
        _bufferWriter.Clear();
        _utf8JsonWriter.Reset(_bufferWriter);
        JsonSerializer.Serialize(_utf8JsonWriter, _dto);
        _utf8JsonWriter.Flush();
    }

    /// <summary>Writes the original class with the compiled serializer.</summary>
    [Benchmark]
    public void JsonPushulator_Class()
    {
        _bufferWriter.Clear();
        _utf8JsonWriter.Reset(_bufferWriter);
        JsonPushulator.Push(_dto, _utf8JsonWriter);
        _utf8JsonWriter.Flush();
    }

    /// <summary>Writes the original struct with the compiled serializer.</summary>
    [Benchmark]
    public void JsonPushulator_Struct()
    {
        _bufferWriter.Clear();
        _utf8JsonWriter.Reset(_bufferWriter);
        JsonPushulator.Push(_struct, _utf8JsonWriter);
        _utf8JsonWriter.Flush();
    }

    /// <summary>Writes every original class member directly with Utf8JsonWriter.</summary>
    [Benchmark]
    public void Manual_Utf8JsonWriter_Direct()
    {
        _bufferWriter.Clear();
        _utf8JsonWriter.Reset(_bufferWriter);

        _utf8JsonWriter.WriteStartObject();
        _utf8JsonWriter.WriteNumber("Id", _dto.Id);
        _utf8JsonWriter.WriteNumber("Price", _dto.Price);
        _utf8JsonWriter.WriteNumber("Amount", _dto.Amount);
        _utf8JsonWriter.WriteString("Symbol", _dto.Symbol);
        _utf8JsonWriter.WriteNumber("Timestamp", _dto.Timestamp);
        _utf8JsonWriter.WriteBoolean("IsBuyerMaker", _dto.IsBuyerMaker);
        _utf8JsonWriter.WriteString("CreatedAt", _dto.CreatedAt);
        _utf8JsonWriter.WriteEndObject();

        _utf8JsonWriter.Flush();
    }
    /// <summary>Disposes the reusable writer after the benchmark case.</summary>
    [GlobalCleanup]
    public void Cleanup() => _utf8JsonWriter.Dispose();
}
