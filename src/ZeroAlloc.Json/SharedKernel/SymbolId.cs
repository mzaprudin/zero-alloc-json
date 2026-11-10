#pragma warning disable MA0048

using System.Runtime.CompilerServices;

namespace GapSonar.Core.Shared;

public readonly record struct SymbolKey(string Base, string Quote)
{
    // Кэшируем хэш один раз при создании ключа
    public ulong Hash { get; } = ComputeFastHash(Base, Quote);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeFastHash(string a, string b)
    {
        ulong hash = 0xcbf29ce484222325; // FNV-1a offset
        const ulong prime = 0x100000001b3;

        ReadOnlySpan<char> spanA = a.AsSpan();
        for (int i = 0; i < spanA.Length; i++)
            hash = (hash ^ spanA[i]) * prime;

        hash = (hash ^ '-') * prime;

        ReadOnlySpan<char> spanB = b.AsSpan();
        for (int i = 0; i < spanB.Length; i++)
            hash = (hash ^ spanB[i]) * prime;

        return hash;
    }

    public override string ToString()
    {
        if (Base is null || Quote is null) return "Unknown";

        int len = Base.Length + 1 + Quote.Length;
        // Use stack allocation for small strings to avoid GC pressure
        Span<char> buffer = len <= 64 ? stackalloc char[len] : new char[len];

        int written = 0;
        Base.AsSpan().CopyTo(buffer);
        written += Base.Length;

        buffer[written++] = '-';

        Quote.AsSpan().CopyTo(buffer[written..]);

        return global::GapSonar.Core.Shared.Shared.GlobalInternDelegate(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        // Use the precomputed hash for O(1) dictionary lookups
        return (int)Hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(SymbolKey other)
    {
        // Fast fail/success based on hash first
        if (Hash != other.Hash) return false;

        // Full check to avoid collisions
        return string.Equals(Base, other.Base, StringComparison.Ordinal)
            && string.Equals(Quote, other.Quote, StringComparison.Ordinal);
    }
}

/// <summary>
/// Structured symbol identifier representing a trading pair
/// Provides both native (exchange-specific) and normalized representations
/// </summary>
public readonly record struct SymbolId(
    ExchangeId ExchangeId,
    SymbolKey Key,
    SymbolType Type,
    string NativeSymbol
)
{
    public static readonly SymbolId Default = new(ExchangeId.Unknown, default, SymbolType.Spot, string.Empty);

    public ulong Hash { get; } = ComputeFastHash(Key.Hash, ExchangeId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeFastHash(ulong hash, ExchangeId id)
    {
        const ulong prime = 0x100000001b3;
        hash = (hash ^ (byte)id) * prime;
        return hash;
    }

    // Строку создаём только когда нужно
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetNormalized(Func<ReadOnlySpan<char>, string> intern)
    {
        if (Key.Base is null || Key.Quote is null) return "Unknown";

        int totalLength = Key.Base.Length + 1 + Key.Quote.Length;
        Span<char> buffer = stackalloc char[totalLength];

        int pos = 0;
        Key.Base.AsSpan().CopyTo(buffer);
        pos += Key.Base.Length;
        buffer[pos++] = '-';
        Key.Quote.AsSpan().CopyTo(buffer[pos..]);

        return intern(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetFullSymbol(Func<ReadOnlySpan<char>, string> intern)
    {
        if (Key.Base is null || Key.Quote is null) return "Unknown";

        if (Type == SymbolType.Spot)
            return GetNormalized(intern);

        string typeStr = GetTypeString(Type);

        // Base + '-' + Quote + '-' + Type
        int totalLength = Key.Base.Length + 1 + Key.Quote.Length + 1 + typeStr.Length;

        Span<char> buffer = totalLength <= 128
            ? stackalloc char[totalLength]
            : new char[totalLength];

        int pos = 0;
        Key.Base.AsSpan().CopyTo(buffer[pos..]);
        pos += Key.Base.Length;

        buffer[pos++] = '-';

        Key.Quote.AsSpan().CopyTo(buffer[pos..]);
        pos += Key.Quote.Length;

        buffer[pos++] = '-';

        typeStr.AsSpan().CopyTo(buffer[pos..]);

        return intern(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetTypeString(SymbolType type) => type switch
    {
        SymbolType.Spot => nameof(SymbolType.Spot),
        SymbolType.LinearPerpetual => nameof(SymbolType.LinearPerpetual),
        SymbolType.InversePerpetual => nameof(SymbolType.InversePerpetual),
        SymbolType.LinearFuture => nameof(SymbolType.LinearFuture),
        SymbolType.InverseFuture => nameof(SymbolType.InverseFuture),
        SymbolType.Option => nameof(SymbolType.Option),
        _ => type.ToString()
    };

    // public override string ToString() => Type == SymbolType.Spot ? Normalized : string.Intern($"{Normalized}-{Type}");
}
