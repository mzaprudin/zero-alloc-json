using System.Buffers;
using System.Collections.Frozen;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using GapSonar.Core.Shared;

namespace RouterBenchmark;

/// <summary>Original 5,000-symbol, 10,000-message JSON routing comparison.</summary>
[MemoryDiagnoser]
public partial class RouterBenchmarks
{
    /// <summary>Original synthetic symbol count.</summary>
    public const int SYMBOL_COUNT = 5_000;
    /// <summary>Original per-invocation message count.</summary>
    public const int MESSAGE_COUNT = 10_000;
    /// <summary>Original deterministic random seed.</summary>
    private const int RANDOM_SEED = 42;
    /// <summary>Original synthetic price scale.</summary>
    private const double PRICE_SCALE = 100_000;
    /// <summary>Original synthetic quantity scale.</summary>
    private const double QUANTITY_SCALE = 10;
    private JsonMessageRouter _jsonRouter = null!;
    private CustomMessageRouter _customRouter = null!;
    private RegexMessageRouter _regexRouter = null!;
    private ReadOnlySequence<byte>[] _messages = null!;
    private FrozenDictionary<string, SymbolId> _frozenNativeMap = null!;

        /// <summary>Builds the original symbol universe and messages and verifies all three routers.</summary>
[GlobalSetup]
    public void Setup()
    {
        // 1. Generate SYMBOL_COUNT symbols
        var dict = new Dictionary<string, SymbolId>();
        var symbols = new List<string>(SYMBOL_COUNT);

        for (int i = 0; i < SYMBOL_COUNT; i++)
        {
            string baseAsset = $"COIN{i}";
            string quoteAsset = "USDT";
            string symbolStr = $"{baseAsset}_{quoteAsset}";

            var key = new SymbolKey(baseAsset, quoteAsset);
            var id = new SymbolId(ExchangeId.MexcFutures, key, SymbolType.LinearPerpetual, symbolStr);

            dict[symbolStr] = id;
            symbols.Add(symbolStr);
        }
        _frozenNativeMap = dict.ToFrozenDictionary();

        // 2. Create Delegate
        SymbolLookup lookup = (ReadOnlySpan<byte> bytes, out SymbolId id) =>
        {
            int maxCharCount = Encoding.UTF8.GetMaxCharCount(bytes.Length);
            Span<char> chars = stackalloc char[maxCharCount];
            int actualChars = Encoding.UTF8.GetChars(bytes, chars);
            return _frozenNativeMap.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(chars[..actualChars], out id);
        };

        // 3. Create Routers
        _jsonRouter = new JsonMessageRouter(lookup);
        _customRouter = new CustomMessageRouter(lookup);
        _regexRouter = new RegexMessageRouter(lookup);

        // 4. Generate MESSAGE_COUNT messages
        _messages = new ReadOnlySequence<byte>[MESSAGE_COUNT];
        var random = new Random(RANDOM_SEED);

        for (int i = 0; i < MESSAGE_COUNT; i++)
        {
            string symbol = symbols[i % SYMBOL_COUNT];
            double price = random.NextDouble() * PRICE_SCALE;
            double qty = random.NextDouble() * QUANTITY_SCALE;
            long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            string json = string.Create(System.Globalization.CultureInfo.InvariantCulture, $$"""{"symbol":"{{symbol}}","data":{"p":{{price:F2}},"q":{{qty:F3}},"T":1},"channel":"push.deal","ts":{{ts}}}""");

            _messages[i] = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json));
        }
        ValidateRoutes();
    }

    /// <summary>Checks every generated message resolves to the expected symbol before timing.</summary>
    private void ValidateRoutes()
    {
        for (int i = 0; i < _messages.Length; i++)
        {
            var expected = _frozenNativeMap[$"COIN{i % SYMBOL_COUNT}_USDT"];
            if (!_jsonRouter.Route(_messages[i], out var standard, out _) || standard != expected ||
                !_customRouter.Route(_messages[i], out var custom, out _) || custom != expected ||
                !_regexRouter.Route(_messages[i], out var regex, out _) || regex != expected)
                throw new InvalidDataException($"Original routers disagree at message {i}.");
        }
    }

        /// <summary>Routes the original message set, normalized per logical message.</summary>
[Benchmark(OperationsPerInvoke = MESSAGE_COUNT, Baseline = true)]
    public void Route10kMessages_JsonReader()
    {
        for (int i = 0; i < _messages.Length; i++)
        {
            _jsonRouter.Route(_messages[i], out _, out _);
        }
    }

        /// <summary>Routes the original message set, normalized per logical message.</summary>
[Benchmark(OperationsPerInvoke = MESSAGE_COUNT)]
    public void Route10kMessages_Custom()
    {
        for (int i = 0; i < _messages.Length; i++)
        {
            _customRouter.Route(_messages[i], out _, out _);
        }
    }

        /// <summary>Routes the original message set, normalized per logical message.</summary>
[Benchmark(OperationsPerInvoke = MESSAGE_COUNT)]
    public void Route10kMessages_Regex()
    {
        for (int i = 0; i < _messages.Length; i++)
        {
            _regexRouter.Route(_messages[i], out _, out _);
        }
    }
}

