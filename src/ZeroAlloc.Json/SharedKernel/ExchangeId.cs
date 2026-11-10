namespace GapSonar.Core.Shared;

/// <summary>
/// Identifies a supported exchange venue and market segment.
/// </summary>
public enum ExchangeId : byte
{
    Unknown = 0,
    MexcSpot = 1,
    MexcFutures = 2,
    GateSpot = 3,
    GateFutures = 4,
    BybitSpot = 5,
    BybitFutures = 6,
    BitgetSpot = 7,
    BitgetFutures = 8,
    BinanceSpot = 9,
    BinanceFutures = 10,
    KrakenSpot = 11,
    KrakenFutures = 12,
    KucoinSpot = 13,
    KucoinFutures = 14,
    OkxSpot = 15,
    OkxFutures = 16,
    AsterdexSpot = 17,
    AsterdexFutures = 18,
    HyperliquidSpot = 19,
    HyperliquidFutures = 20,
    BingxSpot = 21,
    BingxFutures = 22,
    PhemexSpot = 23,
    PhemexFutures = 24,
    LighterFutures = 25,
    [Obsolete("Use HyperliquidSpot.")]
    HyperLiquidSpot = HyperliquidSpot,
    [Obsolete("Use HyperliquidFutures.")]
    HyperLiquidFutures = HyperliquidFutures,
    MaxExchanges = 26,
}
