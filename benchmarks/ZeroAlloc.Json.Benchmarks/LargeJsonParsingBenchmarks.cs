using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using GapSonar.Core.Shared;
using GapSonar.Core.Shared.Infrastructure.Serialization;
using JsonPopulatorBenchmark;

namespace GapSonar.TransportLab.Benchmarks;

/// <summary>Benchmarks parsing a larger single JSON payload (1312 bytes, 50 fields) representing a rich contract message.</summary>
[MemoryDiagnoser]
public class LargeJsonParsingBenchmarks
{
    private byte[] _payload = null!;
    private readonly GateContract _reused = new();
    private GateContractStruct _reusedStruct = new();
    private readonly SimpleStringInterner _interner = new();
    private StringInternerDelegate _intern = null!;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    [GlobalSetup]
    public void Setup()
    {
        _payload = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fixtures", "gate.contract.json"));
        _intern = _interner.Intern;
        JsonPopulator.Populate(_payload, _reused, _intern);
        JsonPopulator.Populate(_payload, ref _reusedStruct, _intern);

        var baseline = JsonSerializer.Deserialize<GateContract>(_payload, _jsonOptions)!;
        if (_reused.Name != baseline.Name || _reused.LastPrice != baseline.LastPrice || _reused.Type != baseline.Type)
            throw new InvalidDataException("Parser benchmark results differ.");
    }

    [Benchmark(Baseline = true)]
    public GateContract SystemTextJson() => JsonSerializer.Deserialize<GateContract>(_payload, _jsonOptions)!;

    [Benchmark]
    public GateContract PopulatorNewObject()
    {
        var value = new GateContract();
        JsonPopulator.Populate(_payload, value);
        return value;
    }

    [Benchmark]
    public GateContract PopulatorReusedObject()
    {
        JsonPopulator.Populate(_payload, _reused);
        return _reused;
    }

    [Benchmark]
    public GateContract PopulatorReusedInterned()
    {
        JsonPopulator.Populate(_payload, _reused, _intern);
        return _reused;
    }

    [Benchmark]
    public GateContractStruct PopulatorStructRef()
    {
        JsonPopulator.Populate(_payload, ref _reusedStruct, _intern);
        return _reusedStruct;
    }
}
