namespace GapSonar.Core.Shared;

/// <summary>
/// Classifies the market contract type for an exchange symbol.
/// </summary>
public enum SymbolType : byte
{
    Spot = 0,
    LinearPerpetual = 1,
    InversePerpetual = 2,
    LinearFuture = 3,
    InverseFuture = 4,
    Option = 5,
}
