using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using GapSonar.Core.Shared;
using GapSonar.Core.Shared.Infrastructure.Collections;
using GapSonar.Core.Shared.Infrastructure.Serialization;

namespace JsonPopulatorBenchmark;

/// <summary>Original GapSonar DTO and Gate contract population workloads.</summary>
[MemoryDiagnoser]
public class PopulatorBenchmarks
{
    /// <summary>Original preallocated contract-list capacity.</summary>
    private const int CONTRACT_CAPACITY = 1_000;
    private byte[] _jsonBytes = null!;
    private byte[] _gateJsonBytes = null!;
    private TradeDto _reusedDto = new();
    private TradeStruct _reusedStruct = new();
    private FastList<GateContract> _reusedGateList = new(CONTRACT_CAPACITY);
    private FastList<GateContractStruct> _reusedGateStructList = new(CONTRACT_CAPACITY);

    /// <summary>Initializes and warms the original benchmark data outside measurements.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var json = """
        {
            "Id": 123456789,
            "Price": 12345.6789,
            "Amount": 0.00123456,
            "Symbol": "BTCUSDT",
            "Timestamp": 1678900000000,
            "IsBuyerMaker": true,
            "CreatedAt": "2023-01-01T12:00:00Z"
        }
        """;
        _jsonBytes = Encoding.UTF8.GetBytes(json);

        _gateJsonBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fixtures", "gate.futures.contracts.all.json"));

        // Warmup
        JsonPopulator.Populate(_jsonBytes, _reusedDto, global::GapSonar.Core.Shared.Shared.GlobalInternDelegate);
        JsonPopulator.Populate(_jsonBytes, ref _reusedStruct);
        JsonPopulator.Populate(_gateJsonBytes, _reusedGateList, global::GapSonar.Core.Shared.Shared.GlobalInternDelegate);
        JsonPopulator.Populate(_gateJsonBytes, _reusedGateStructList, global::GapSonar.Core.Shared.Shared.GlobalInternDelegate);
        ValidatePopulatedValues();
    }

    /// <summary>Checks original workloads against the framework parser before timing.</summary>
    private void ValidatePopulatedValues()
    {
        var expected = JsonSerializer.Deserialize<TradeDto>(_jsonBytes)!;
        if (JsonSerializer.Serialize(expected) != JsonSerializer.Serialize(_reusedDto))
            throw new InvalidDataException("Original DTO population differs from its baseline.");
        var expectedStruct = JsonSerializer.Deserialize<TradeStruct>(_jsonBytes);
        if (JsonSerializer.Serialize(expectedStruct) != JsonSerializer.Serialize(_reusedStruct))
            throw new InvalidDataException("Original struct population differs from its baseline.");
        var contracts = JsonSerializer.Deserialize<List<GateContract>>(_gateJsonBytes, _jsonOptions)!;
        if (contracts.Count != _reusedGateList.Count || contracts.Count != _reusedGateStructList.Count)
            throw new InvalidDataException("Original contract-list counts differ.");
        for (int i = 0; i < contracts.Count; i++)
        {
            var actual = _reusedGateStructList[i];
            var expectedContract = contracts[i];
            if (JsonSerializer.Serialize(expectedContract) != JsonSerializer.Serialize(_reusedGateList[i]) ||
                actual.Name != expectedContract.Name || actual.Type != expectedContract.Type ||
                actual.LastPrice != expectedContract.LastPrice || actual.MarkPrice != expectedContract.MarkPrice ||
                actual.IndexPrice != expectedContract.IndexPrice || actual.FundingRate != expectedContract.FundingRate ||
                actual.Status != expectedContract.Status || actual.LeverageMax != expectedContract.LeverageMax ||
                actual.LeverageMin != expectedContract.LeverageMin)
                throw new InvalidDataException($"Original contract values differ at index {i}.");
        }
    }

    /// <summary>Deserializes a fresh original TradeDto using System.Text.Json.</summary>
    [Benchmark(Baseline = true)]
    public TradeDto Standard_Deserialize_Class()
    {
        return JsonSerializer.Deserialize<TradeDto>(_jsonBytes)!;
    }

    /// <summary>Populates the reused original class with warmed symbol interning.</summary>
    [Benchmark]
    public void JsonPopulator_Class_Reused_Interned()
    {
        JsonPopulator.Populate(_jsonBytes, _reusedDto, global::GapSonar.Core.Shared.Shared.GlobalInternDelegate);
    }

    /// <summary>Populates the original struct by reference; it intentionally has no Symbol member.</summary>
    [Benchmark]
    public void JsonPopulator_Struct_Ref()
    {
        JsonPopulator.Populate(_jsonBytes, ref _reusedStruct);
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    /// <summary>Deserializes the original complete Gate fixture to a fresh list.</summary>
    [Benchmark]
    public List<GateContract> Gate_Standard_Deserialize_List()
    {
        return JsonSerializer.Deserialize<List<GateContract>>(_gateJsonBytes, _jsonOptions)!;
    }

    /// <summary>Populates original Gate classes in preallocated storage.</summary>
    [Benchmark]
    public void Gate_JsonPopulator_FastList_Reused_Interned()
    {
        JsonPopulator.Populate(_gateJsonBytes, _reusedGateList, global::GapSonar.Core.Shared.Shared.GlobalInternDelegate);
    }

    /// <summary>Populates original Gate structs in preallocated storage.</summary>
    [Benchmark]
    public void Gate_JsonPopulator_FastList_Reused_Interned_Struct()
    {
        JsonPopulator.Populate(_gateJsonBytes, _reusedGateStructList, global::GapSonar.Core.Shared.Shared.GlobalInternDelegate);
    }
}
