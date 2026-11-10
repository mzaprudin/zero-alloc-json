using System.Buffers;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using GapSonar.Core.Shared;
using GapSonar.Core.Shared.Infrastructure.Serialization;
using GapSonar.TransportLab.Support;

namespace GapSonar.TransportLab.Benchmarks;

/// <summary>Compares identical JSON fields; reuse and interning are explicit in method names.</summary>
[MemoryDiagnoser]
public class JsonParsingBenchmarks
{
    /// <summary>UTF-8 input loaded outside timed iterations.</summary>
    private byte[] _payload = null!;
    /// <summary>Caller-owned reusable destination.</summary>
    private readonly Trade _reused = new();
    /// <summary>Session-owned interner, warmed before measurements.</summary>
    private readonly SimpleStringInterner _interner = new();
    /// <summary>Cached delegate avoids per-call method-group allocation.</summary>
    private StringInternerDelegate _intern = null!;

    /// <summary>Loads the fixture and checks equivalence before benchmarking.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _payload = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fixtures", "trade.json"));
        _intern = _interner.Intern;
        JsonPopulator.Populate(_payload, _reused, _intern);
        var baseline = JsonSerializer.Deserialize(_payload, TradeJsonContext.Default.Trade)!;
        if (JsonSerializer.Serialize(_reused) != JsonSerializer.Serialize(baseline))
            throw new InvalidDataException("Parser benchmark results differ.");
    }

    /// <summary>Framework reflection metadata cached after warmup; creates a fresh object and string.</summary>
    [Benchmark(Baseline = true)]
    public Trade SystemTextJson() => JsonSerializer.Deserialize<Trade>(_payload)!;
    /// <summary>Generated metadata baseline; creates the same object and string.</summary>
    [Benchmark]
    public Trade SystemTextJsonGenerated() => JsonSerializer.Deserialize(_payload, TradeJsonContext.Default.Trade)!;
    /// <summary>Same fresh-object and fresh-string ownership as the framework baseline.</summary>
    [Benchmark]
    public Trade PopulatorNewObject()
    {
        var value = new Trade();
        JsonPopulator.Populate(_payload, value);
        return value;
    }
    /// <summary>Caller reuses the object; the symbol string is still allocated.</summary>
    [Benchmark]
    public Trade PopulatorReusedObject()
    {
        JsonPopulator.Populate(_payload, _reused);
        return _reused;
    }
    /// <summary>Caller reuses the object and a previously seen symbol.</summary>
    [Benchmark]
    public Trade PopulatorReusedInterned()
    {
        JsonPopulator.Populate(_payload, _reused, _intern);
        return _reused;
    }
}

/// <summary>Compares identical outputs with the same reusable writer and buffer lifecycle.</summary>
[MemoryDiagnoser]
public class JsonWritingBenchmarks
{
    /// <summary>Initial buffer capacity, sufficient for the fixture.</summary>
    private const int WRITER_CAPACITY_BYTES = 1_024;
    /// <summary>Same object for every writer.</summary>
    private Trade _trade = null!;
    /// <summary>Reusable output buffer.</summary>
    private readonly ArrayBufferWriter<byte> _buffer = new(WRITER_CAPACITY_BYTES);
    /// <summary>Reusable validating JSON writer.</summary>
    private Utf8JsonWriter _writer = null!;

    /// <summary>Checks output equality before measurements.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fixtures", "trade.json"));
        _trade = JsonSerializer.Deserialize(bytes, TradeJsonContext.Default.Trade)!;
        _writer = new Utf8JsonWriter(_buffer);
        SystemTextJson();
        var expected = _buffer.WrittenSpan.ToArray();
        PushulatorReusedWriter();
        if (!expected.AsSpan().SequenceEqual(_buffer.WrittenSpan)) throw new InvalidDataException("Writer outputs differ.");
        SystemTextJsonGenerated();
        if (!expected.AsSpan().SequenceEqual(_buffer.WrittenSpan)) throw new InvalidDataException("Generated writer output differs.");
    }

    /// <summary>Framework serializer using the existing writer and output buffer.</summary>
    [Benchmark(Baseline = true)]
    public int SystemTextJson()
    {
        Reset();
        JsonSerializer.Serialize(_writer, _trade);
        _writer.Flush();
        return _buffer.WrittenCount;
    }
    /// <summary>Source-generated framework serializer with identical buffer reuse.</summary>
    [Benchmark]
    public int SystemTextJsonGenerated()
    {
        Reset();
        JsonSerializer.Serialize(_writer, _trade, TradeJsonContext.Default.Trade);
        _writer.Flush();
        return _buffer.WrittenCount;
    }
    /// <summary>Compiled serializer with identical buffer reuse.</summary>
    [Benchmark]
    public int PushulatorReusedWriter()
    {
        Reset();
        JsonPushulator.Push(_trade, _writer);
        _writer.Flush();
        return _buffer.WrittenCount;
    }
    /// <summary>Resets both buffer and writer for each implementation.</summary>
    private void Reset()
    {
        _buffer.Clear();
        _writer.Reset(_buffer);
    }
    /// <summary>Releases the reused writer after the job.</summary>
    [GlobalCleanup]
    public void Cleanup() => _writer.Dispose();
}
