using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GapSonar.Core.Shared;

namespace RouterBenchmark;

/// <summary>Original Utf8JsonReader symbol extraction baseline.</summary>
public sealed class JsonMessageRouter(SymbolLookup symbolLookup) : IMessageRouter<int>
{
    private readonly SymbolLookup _symbolLookup = symbolLookup;

    /// <summary>Extracts a symbol using the original measured algorithm.</summary>
    public bool Route(ReadOnlySequence<byte> message, out SymbolId symbolId, out int context)
    {
        symbolId = default;
        context = 0;
        var reader = new Utf8JsonReader(message);

        try
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("symbol"u8))
                {
                    if (reader.Read() && reader.TokenType == JsonTokenType.String)
                    {
                        return _symbolLookup(reader.ValueSpan, out symbolId);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Ignore malformed JSON or partial frames
        }

        return false;
    }
}

/// <summary>Original generated-regex symbol extraction baseline.</summary>
public sealed partial class RegexMessageRouter(SymbolLookup symbolLookup) : IMessageRouter<int>
{
    private readonly SymbolLookup _symbolLookup = symbolLookup;

    // Use a slightly different regex optimized for EnumerateMatches (no capturing groups needed, just match the whole value)
    // We match "symbol":"VALUE" and then manually slice the value
    [GeneratedRegex("\"symbol\":\"[^\"]+\"")]
    private static partial Regex SymbolRegex();

    /// <summary>Extracts a symbol using the original measured algorithm.</summary>
    public bool Route(ReadOnlySequence<byte> message, out SymbolId symbolId, out int context)
    {
        symbolId = default;
        context = 0;
        ReadOnlySpan<byte> byteSpan = message.IsSingleSegment ? message.FirstSpan : message.ToArray();

        // Stack allocate chars for Regex (Zero Heap Alloc)
        int maxCharCount = Encoding.UTF8.GetMaxCharCount(byteSpan.Length);
        Span<char> charSpan = stackalloc char[maxCharCount];
        int charCount = Encoding.UTF8.GetChars(byteSpan, charSpan);
        ReadOnlySpan<char> chars = charSpan[..charCount];

        // Zero-Allocation Regex Search
        foreach (var match in SymbolRegex().EnumerateMatches(chars))
        {
            // match.Index and match.Length refer to the whole match: "symbol":"BTC_USDT"
            // We know "symbol":" is 10 chars long
            const int SYMBOL_VALUE_OFFSET = 10;
            int valueStart = match.Index + SYMBOL_VALUE_OFFSET;
            const int SYMBOL_MATCH_OVERHEAD = 11;
            int valueLength = match.Length - SYMBOL_MATCH_OVERHEAD; // -10 for prefix, -1 for closing quote "

            if (valueLength > 0)
            {
                // Map char indices back to byte indices (Assumes ASCII-compatible for these control chars, which is true for JSON)
                // Since we are looking up in a dictionary that supports AlternateLookup<Span<char>>,
                // we can just use the char span directly!

                // IMPORTANT: AbstractExchange lookup expects Span<char> anyway.
                // But the interface requires us to pass something that the delegate can handle.
                // Our delegate setup in Benchmark:
                // (ReadOnlySpan<byte> bytes, out SymbolId id) => ... Encoding.UTF8.GetChars ...

                // This is tricky. The Router interface takes Bytes. The Delegate takes Bytes.
                // But Regex works on Chars.

                // OPTIMIZATION: We can skip the delegate's internal decoding if we already have chars.
                // But for fair comparison with the Interface contract, we must provide bytes to the delegate.
                // Luckily, for ASCII/UTF8 JSON "symbol":"...", byte index == char index mostly.
                // Let's assume 1-to-1 mapping for the indices found by Regex to slice the original ByteSpan.

                return _symbolLookup(byteSpan.Slice(valueStart, valueLength), out symbolId);
            }
        }

        return false;
    }
}

/// <summary>Original Span.IndexOf extraction of compact ASCII symbol fields.</summary>
public sealed class CustomMessageRouter(SymbolLookup symbolLookup) : IMessageRouter<int>
{
    private static readonly byte[] SYMBOL_MARKER = "\"symbol\":\""u8.ToArray();
    private readonly SymbolLookup _symbolLookup = symbolLookup;

    /// <summary>Extracts a symbol using the original measured algorithm.</summary>
    public bool Route(ReadOnlySequence<byte> message, out SymbolId symbolId, out int context)
    {
        ReadOnlySpan<byte> span = message.IsSingleSegment ? message.FirstSpan : message.ToArray();
        context = 0;

        int idx = span.IndexOf(SYMBOL_MARKER);
        if (idx >= 0)
        {
            int valueStart = idx + SYMBOL_MARKER.Length;
            var remaining = span[valueStart..];
            int endIdx = remaining.IndexOf((byte)'"');
            if (endIdx >= 0)
            {
                return _symbolLookup(remaining[..endIdx], out symbolId);
            }
        }

        symbolId = default;
        return false;
    }
}
