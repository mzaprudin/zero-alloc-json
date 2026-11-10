using System.Buffers;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace GapSonar.Core.Shared;

public static class Shared
{
    public static readonly SimpleStringInterner GlobalInterner = new();
    public static readonly StringInternerDelegate GlobalInternDelegate = GlobalInterner.Intern;
    public static string? GetStringInterned(ref Utf8JsonReader reader) => AbstractParser.GetStringInterned(ref reader, GlobalInternDelegate);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ParseHighPrecisionTimestamp(ReadOnlySpan<char> tsSpan)
    {
        if (tsSpan.IsEmpty) return default;

        int dotIdx = tsSpan.IndexOf('.');
        if (dotIdx != -1)
        {
            if (long.TryParse(tsSpan[..dotIdx], CultureInfo.InvariantCulture, out long val))
            {
                // Heuristic: If < 10 billion, it's seconds.
                var dt = val < 10_000_000_000L
                    ? DateTimeOffset.FromUnixTimeSeconds(val).UtcDateTime
                    : DateTimeOffset.FromUnixTimeMilliseconds(val).UtcDateTime;

                ReadOnlySpan<char> fracSpan = tsSpan[(dotIdx + 1)..];
                if (long.TryParse(fracSpan, CultureInfo.InvariantCulture, out long frac))
                {
                    // Convert fraction to Ticks
                    // If 'val' was seconds, frac is fraction of a second.
                    // If 'val' was ms, frac is fraction of a millisecond.
                    // To stay consistent with FromUnixTime, let's treat frac as
                    // fraction of the UNIT used.

                    if (val < 10_000_000_000L)
                    {
                        // Fraction of a second (1s = 10,000,000 ticks)
                        long ticks = fracSpan.Length switch
                        {
                            1 => frac * 1000000,
                            2 => frac * 100000,
                            3 => frac * 10000,
                            4 => frac * 1000,
                            5 => frac * 100,
                            6 => frac * 10,
                            7 => frac,
                            _ => (long)(frac * 10000000 / Math.Pow(10, fracSpan.Length))
                        };
                        return dt.AddTicks(ticks);
                    }
                    else
                    {
                        // Fraction of a millisecond (1ms = 10,000 ticks)
                        long ticks = fracSpan.Length switch
                        {
                            1 => frac * 1000,
                            2 => frac * 100,
                            3 => frac * 10,
                            4 => frac,
                            5 => frac / 10,
                            6 => frac / 100,
                            7 => frac / 1000,
                            _ => (long)(frac * 10000 / Math.Pow(10, fracSpan.Length))
                        };
                        return dt.AddTicks(ticks);
                    }
                }
                return dt;
            }
        }
        else if (long.TryParse(tsSpan, CultureInfo.InvariantCulture, out long val))
        {
            return val < 10_000_000_000L
                ? DateTimeOffset.FromUnixTimeSeconds(val).UtcDateTime
                : DateTimeOffset.FromUnixTimeMilliseconds(val).UtcDateTime;
        }

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ParseHighPrecisionTimestamp(ReadOnlySpan<byte> tsSpan)
    {
        // 1. Estimate char count (ASCII/UTF8)
        int maxCharCount = Encoding.UTF8.GetMaxCharCount(tsSpan.Length);
        if (maxCharCount > 128)
            return default;

        // 2. Allocate on Stack (Zero Heap Alloc)
        Span<char> chars = stackalloc char[maxCharCount];

        // 3. Decode
        if (!Encoding.UTF8.TryGetChars(tsSpan, chars, out int actualChars))
            return default;

        // 4. Lookup using Span<char>
        return ParseHighPrecisionTimestamp(chars[..actualChars]);
    }
}

public delegate string StringInternerDelegate(ReadOnlySpan<char> chars);

/// <summary>String decoding shared by parsers, with optional caller-owned interning.</summary>
public static class AbstractParser
{
    /// <summary>Decodes JSON escape sequences before consulting the optional interner.</summary>
    public static string? GetStringInterned(ref Utf8JsonReader reader, StringInternerDelegate? interner)
    {
        if (interner is null) return reader.GetString();
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.ValueIsEscaped)
        {
            const int STACK_CHARACTER_LIMIT = 128;
            int byteLength = checked((int)(reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length));
            char[]? rental = null;
            Span<char> decoded = byteLength <= STACK_CHARACTER_LIMIT
                ? stackalloc char[byteLength]
                : (rental = ArrayPool<char>.Shared.Rent(byteLength));
            try { return interner(decoded[..reader.CopyString(decoded)]); }
            finally { if (rental is not null) ArrayPool<char>.Shared.Return(rental); }
        }

        if (!reader.HasValueSequence)
        {
            var bytes = reader.ValueSpan;
            int maxChars = Encoding.UTF8.GetMaxCharCount(bytes.Length);
            Span<char> chars = maxChars <= 128 ? stackalloc char[maxChars] : new char[maxChars];
            int charCount = Encoding.UTF8.GetChars(bytes, chars);
            return interner(chars[..charCount]);
        }
        else
        {
            int byteLen = (int)reader.ValueSequence.Length;
            Span<byte> tempBytes = byteLen <= 256 ? stackalloc byte[byteLen] : new byte[byteLen];
            reader.ValueSequence.CopyTo(tempBytes);

            int maxChars = Encoding.UTF8.GetMaxCharCount(tempBytes.Length);
            Span<char> chars = maxChars <= 128 ? stackalloc char[maxChars] : new char[maxChars];
            int charCount = Encoding.UTF8.GetChars(tempBytes, chars);
            return interner(chars[..charCount]);
        }
    }
}

public sealed class SimpleStringInterner
{
    private readonly ConcurrentDictionary<string, string> _pool = new(StringComparer.Ordinal);
    private readonly StringInternerDelegate _internDelegate;

    public SimpleStringInterner()
    {
        _internDelegate = Intern;
    }

    public string Intern(ReadOnlySpan<char> chars)
    {
        // .NET 9 feature: AlternateLookup allows querying Dictionary using Span without allocation
        var lookup = _pool.GetAlternateLookup<ReadOnlySpan<char>>();

        if (lookup.TryGetValue(chars, out var existing))
        {
            return existing;
        }

        string newString = new(chars);
        // TryAdd is thread-safe. If another thread added it in the meantime, we get the existing one.
        return _pool.GetOrAdd(newString, newString);
    }

    public string? GetStringInterned(ref Utf8JsonReader reader)
    {
        return AbstractParser.GetStringInterned(ref reader, _internDelegate);
    }
}
