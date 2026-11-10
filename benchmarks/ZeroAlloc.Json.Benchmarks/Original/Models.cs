using System.Text.Json.Serialization;
using GapSonar.Core.Shared.Infrastructure.Serialization;

namespace JsonPopulatorBenchmark;

/// <summary>Original TradeDto benchmark contract.</summary>
public class TradeDto
{
    /// <summary>Original Id contract member.</summary>
    public long Id { get; set; }
    /// <summary>Original Price contract member.</summary>
    public double Price { get; set; }
    /// <summary>Original Amount contract member.</summary>
    public decimal Amount { get; set; }

    /// <summary>Original Symbol contract member.</summary>
    [JsonIntern]
    public string Symbol { get; set; } = "";

    /// <summary>Original Timestamp contract member.</summary>
    public long Timestamp { get; set; }
    /// <summary>Original IsBuyerMaker contract member.</summary>
    public bool IsBuyerMaker { get; set; }
    /// <summary>Original CreatedAt contract member.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Original TradeStruct benchmark contract.</summary>
public struct TradeStruct
{
    /// <summary>Original Id contract member.</summary>
    public long Id { get; set; }
    /// <summary>Original Price contract member.</summary>
    public double Price { get; set; }
    /// <summary>Original Amount contract member.</summary>
    public decimal Amount { get; set; }
    /// <summary>Original Timestamp contract member.</summary>
    public long Timestamp { get; set; }
    /// <summary>Original IsBuyerMaker contract member.</summary>
    public bool IsBuyerMaker { get; set; }
    /// <summary>Original CreatedAt contract member.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Original GateContract benchmark contract.</summary>
public class GateContract
{
    /// <summary>Original Name contract member.</summary>
    [JsonPropertyName("name"), JsonIntern] public string Name { get; set; } = "";
    /// <summary>Original Type contract member.</summary>
    [JsonPropertyName("type"), JsonIntern] public string Type { get; set; } = "";
    /// <summary>Original LastPrice contract member.</summary>
    [JsonPropertyName("last_price")] public double LastPrice { get; set; }
    /// <summary>Original MarkPrice contract member.</summary>
    [JsonPropertyName("mark_price")] public double MarkPrice { get; set; }
    /// <summary>Original IndexPrice contract member.</summary>
    [JsonPropertyName("index_price")] public double IndexPrice { get; set; }
    /// <summary>Original FundingRate contract member.</summary>
    [JsonPropertyName("funding_rate")] public double FundingRate { get; set; }
    /// <summary>Original Status contract member.</summary>
    [JsonPropertyName("status"), JsonIntern] public string Status { get; set; } = "";
    /// <summary>Original LeverageMax contract member.</summary>
    [JsonPropertyName("leverage_max")] public double LeverageMax { get; set; }
    /// <summary>Original LeverageMin contract member.</summary>
    [JsonPropertyName("leverage_min")] public double LeverageMin { get; set; }
}

/// <summary>Original GateContractStruct benchmark contract.</summary>
public struct GateContractStruct
{
    /// <summary>Original Name contract member.</summary>
    [JsonPropertyName("name"), JsonIntern] public string Name;
    /// <summary>Original Type contract member.</summary>
    [JsonPropertyName("type"), JsonIntern] public string Type;
    /// <summary>Original LastPrice contract member.</summary>
    [JsonPropertyName("last_price")] public double LastPrice;
    /// <summary>Original MarkPrice contract member.</summary>
    [JsonPropertyName("mark_price")] public double MarkPrice;
    /// <summary>Original IndexPrice contract member.</summary>
    [JsonPropertyName("index_price")] public double IndexPrice;
    /// <summary>Original FundingRate contract member.</summary>
    [JsonPropertyName("funding_rate")] public double FundingRate;
    /// <summary>Original Status contract member.</summary>
    [JsonPropertyName("status"), JsonIntern] public string Status;
    /// <summary>Original LeverageMax contract member.</summary>
    [JsonPropertyName("leverage_max")] public double LeverageMax;
    /// <summary>Original LeverageMin contract member.</summary>
    [JsonPropertyName("leverage_min")] public double LeverageMin;
}
