using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GapSonar.Core.Shared.Infrastructure.Serialization;

namespace GapSonar.Core.Tests;

public class JsonPushulatorTests
{
    /// <summary>Test-only equivalent of the parent engine's order direction.</summary>
    public enum OrderSide : byte { None = 0, Buy = 1, Sell = 2 }

    private sealed class TestHeaderCollector : IHeaderCollector
    {
        public readonly Dictionary<string, string> Headers = [];

        public void AddHeader(string name, ReadOnlySpan<byte> valueUtf8)
        {
            Headers[name] = Encoding.UTF8.GetString(valueUtf8);
        }

        public void AddHeader(string name, string value)
        {
            Headers[name] = value;
        }
    }

    /// <summary>
    /// Captures every header emission in insertion order, preserving duplicates.
    /// Used by the <c>AddHeaderConverted_*</c> tests to assert that the converter
    /// emits exactly one entry per call (or none, in the null case) and that the
    /// stringified value is byte-for-byte what we expect — no tolerance window.
    /// </summary>
    private sealed class CapturingHeaderCollector : IHeaderCollector
    {
        public readonly List<(string Name, string Value)> Entries = [];

        public void AddHeader(string name, ReadOnlySpan<byte> valueUtf8)
        {
            Entries.Add((name, Encoding.UTF8.GetString(valueUtf8)));
        }

        public void AddHeader(string name, string value)
        {
            Entries.Add((name, value));
        }
    }

    [Fact]
    public void AddHeaderConverted_DateTime_UsesProvidedValueNotUtcNow()
    {
        // Arrange: a DateTime exactly one hour in the past, in UTC.
        // The previous bug ignored the captured value and emitted UtcNow's epoch.
        // Tolerance is intentionally zero — we want a fixed regression guard, not
        // a "close enough" check that could mask another silent re-introduction
        // of UtcNow.
        var collector = new CapturingHeaderCollector();
        var pastUtc = DateTime.UtcNow.AddHours(-1);
        var expectedMs = ((DateTimeOffset)pastUtc).ToUnixTimeMilliseconds();
        var notExpectedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        PushulatorHelper.AddHeaderConverted(collector, "X-TIMESTAMP", pastUtc);

        // Assert
        Assert.Single(collector.Entries);
        var (name, value) = collector.Entries[0];
        Assert.Equal("X-TIMESTAMP", name);
        Assert.Equal(expectedMs.ToString(CultureInfo.InvariantCulture), value);
        Assert.NotEqual(notExpectedMs.ToString(CultureInfo.InvariantCulture), value);
    }

    [Fact]
    public void AddHeaderConverted_NonDateTime_FallsBackToToString()
    {
        // Arrange
        var collector = new CapturingHeaderCollector();
        var guid = Guid.Parse("7f8703ee-5cc5-4d03-94b6-02baf15a91fa");

        // Act: cover a string, an int, and a Guid — the three representative
        // non-DateTime branches that should hit the value.ToString() fallback.
        PushulatorHelper.AddHeaderConverted(collector, "X-STR", "hello");
        PushulatorHelper.AddHeaderConverted(collector, "X-INT", 42);
        PushulatorHelper.AddHeaderConverted(collector, "X-GUID", guid);

        // Assert
        Assert.Equal(3, collector.Entries.Count);
        Assert.Equal(("X-STR", "hello"), collector.Entries[0]);
        Assert.Equal(("X-INT", 42.ToString()), collector.Entries[1]);
        Assert.Equal(("X-GUID", guid.ToString()), collector.Entries[2]);
    }

    [Fact]
    public void AddHeaderConverted_Null_DoesNotEmit()
    {
        // Arrange
        var collector = new CapturingHeaderCollector();

        // Act
        PushulatorHelper.AddHeaderConverted<string?>(collector, "X-NULL", null);

        // Assert
        Assert.Empty(collector.Entries);
    }

    private struct ComplexRequest
    {
        [RequestHeader("X-API-KEY")]
        public FixedString64 ApiKey;

        [RequestHeader("X-SIGNATURE")]
        public FixedString128 Signature;

        [JsonPropertyName("symbol")]
        public SymbolId Symbol;

        [JsonPropertyName("price"), JsonToString]
        public double Price;

        [JsonPropertyName("amount")]
        public double Qty;

        [JsonPropertyName("side"), JsonToString]
        public OrderSide Side;

        [JsonPropertyName("time"), JsonToMs]
        public DateTime Timestamp;

        [JsonPropertyName("timeStr"), JsonToStringMs]
        public DateTime TimestampStr;

        [JsonPropertyName("active")]
        public bool IsActive;

        [JsonPropertyName("activeStr"), JsonToString]
        public bool IsActiveStr;

        [JsonIgnore]
        public string InternalNote;
    }

    [Fact]
    public void Push_ShouldSerializeBodyAndIgnoreHeadersAndIgnoredMembers()
    {
        // Arrange
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        var ts = new DateTime(2026, 2, 18, 12, 0, 0, DateTimeKind.Utc);

        var request = new ComplexRequest
        {
            ApiKey = new FixedString64(),
            Symbol = new SymbolId(ExchangeId.BybitFutures, new SymbolKey("BTC", "USDT"), SymbolType.LinearPerpetual, "BTCUSDT"),
            Price = 50000.5,
            Qty = 0.1,
            Side = OrderSide.Buy,
            Timestamp = ts,
            TimestampStr = ts,
            IsActive = true,
            IsActiveStr = true,
            InternalNote = "Should be ignored"
        };
        request.ApiKey.Set("my-api-key"u8);

        // Act
        JsonPushulator.Push(request, writer);
        writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Console.WriteLine($"Generated JSON: {json}");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Headers should NOT be in JSON
        Assert.False(root.TryGetProperty("ApiKey", out _));
        Assert.False(root.TryGetProperty("X-API-KEY", out _));

        // Body members
        Assert.Equal("BTCUSDT", root.GetProperty("symbol").GetString());
        Assert.Equal("50000.5", root.GetProperty("price").GetString()); // JsonToString
        Assert.Equal(0.1, root.GetProperty("amount").GetDouble());
        Assert.Equal("Buy", root.GetProperty("side").GetString());

        var expectedMs = new DateTimeOffset(ts).ToUnixTimeMilliseconds();
        Assert.Equal(expectedMs, root.GetProperty("time").GetInt64()); // JsonToMs
        Assert.Equal(expectedMs.ToString(), root.GetProperty("timeStr").GetString()); // JsonToStringMs

        Assert.True(root.GetProperty("active").GetBoolean());
        Assert.Equal("true", root.GetProperty("activeStr").GetString()); // JsonToString

        // Ignored members
        Assert.False(root.TryGetProperty("InternalNote", out _));
    }

    [Fact]
    public void CollectHeaders_ShouldCollectOnlyHeaderMembers()
    {
        // Arrange
        var collector = new TestHeaderCollector();
        var request = new ComplexRequest
        {
            ApiKey = new FixedString64(),
            Signature = new FixedString128(),
            Qty = 1.0 // Should be ignored by collector
        };
        request.ApiKey.Set("api-123"u8);
        request.Signature.Set("sign-456"u8);

        // Act
        JsonPushulator.CollectHeaders(request, collector);

        // Assert
        Assert.Equal(2, collector.Headers.Count);
        Assert.Equal("api-123", collector.Headers["X-API-KEY"]);
        Assert.Equal("sign-456", collector.Headers["X-SIGNATURE"]);
        Assert.False(collector.Headers.ContainsKey("amount"));
    }

    private struct SimpleTypesRequest
    {
        [JsonPropertyName("i")] public int IntVal;
        [JsonPropertyName("l")] public long LongVal;
        [JsonPropertyName("d")] public double DoubleVal;
        [JsonPropertyName("m")] public decimal DecimalVal;
        [JsonPropertyName("s")] public string StringVal;
        [JsonPropertyName("fs")] public FixedString8 FixedStr;
    }

    [Fact]
    public void Push_ShouldHandleAllSimpleTypes()
    {
        // Arrange
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        var request = new SimpleTypesRequest
        {
            IntVal = 42,
            LongVal = 9999999999L,
            DoubleVal = 123.456,
            DecimalVal = 789.01m,
            StringVal = "hello",
            FixedStr = new FixedString8()
        };
        request.FixedStr.Set("test"u8);

        // Act
        JsonPushulator.Push(request, writer);
        writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(42, root.GetProperty("i").GetInt32());
        Assert.Equal(9999999999L, root.GetProperty("l").GetInt64());
        Assert.Equal(123.456, root.GetProperty("d").GetDouble());
        Assert.Equal(789.01m, root.GetProperty("m").GetDecimal());
        Assert.Equal("hello", root.GetProperty("s").GetString());
        Assert.Equal("test", root.GetProperty("fs").GetString());
    }

    private struct NestedObject
    {
        [JsonPropertyName("id")] public int Id;
        [JsonPropertyName("meta")] public MetaInfo Meta;
        [JsonPropertyName("tags")] public FastList<FixedString16> Tags;
    }

    private struct MetaInfo
    {
        [JsonPropertyName("v")] public int Version;
    }

    [Fact]
    public void Push_ShouldHandleNestedObjectsAndFastList()
    {
        // Arrange
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        var request = new NestedObject
        {
            Id = 1,
            Meta = new MetaInfo { Version = 2 },
            Tags = new FastList<FixedString16>(4)
        };
        var tag1 = new FixedString16(); tag1.Set("tag1"u8);
        var tag2 = new FixedString16(); tag2.Set("tag2"u8);
        request.Tags.Add(tag1);
        request.Tags.Add(tag2);

        // Act
        JsonPushulator.Push(request, writer);
        writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(1, root.GetProperty("id").GetInt32());
        Assert.Equal(2, root.GetProperty("meta").GetProperty("v").GetInt32());

        var tags = root.GetProperty("tags");
        Assert.Equal(2, tags.GetArrayLength());
        Assert.Equal("tag1", tags[0].GetString());
        Assert.Equal("tag2", tags[1].GetString());
    }

    [JsonAsArray]
    private struct PositionalArgs
    {
        public FixedString32 Key;
        [JsonToString] public long Expires;
        public string Signature;
    }

    [Fact]
    public void Push_ShouldHandleJsonAsArray()
    {
        // Arrange
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        var args = new PositionalArgs
        {
            Key = new FixedString32("my-key"),
            Expires = 123456789,
            Signature = "my-sign"
        };

        // Act
        JsonPushulator.Push(args, writer);
        writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(3, root.GetArrayLength());
        Assert.Equal("my-key", root[0].GetString());
        Assert.Equal("123456789", root[1].GetString());
        Assert.Equal("my-sign", root[2].GetString());
    }

    private struct InlineArrayWrapper
    {
        [JsonPropertyName("op")] public string Op;
        [JsonPropertyName("args"), JsonInlineArray] public MetaInfo Args;
    }

    [Fact]
    public void Push_ShouldHandleJsonInlineArray()
    {
        // Arrange
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        var request = new InlineArrayWrapper
        {
            Op = "test",
            Args = new MetaInfo { Version = 42 }
        };

        // Act
        JsonPushulator.Push(request, writer);
        writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("test", root.GetProperty("op").GetString());
        var args = root.GetProperty("args");
        Assert.Equal(JsonValueKind.Array, args.ValueKind);
        Assert.Equal(1, args.GetArrayLength());
        Assert.Equal(42, args[0].GetProperty("v").GetInt32());
    }

    private enum TestEnum { ValueOne = 1, ValueTwo = 2 }
    private struct EnumRequest { [JsonPropertyName("e")] public TestEnum E; }

    [Fact]
    public void Push_ShouldHandleEnumCaching()
    {
        // Arrange
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        var req = new EnumRequest { E = TestEnum.ValueTwo };

        // Act
        JsonPushulator.Push(req, writer);
        writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("ValueTwo", doc.RootElement.GetProperty("e").GetString());
    }

    private enum TestDottedEnum
    {
        Unknown = 0,
        [JsonPropertyName("order.linear")] OrderLinear = 1,
        [JsonPropertyName("position.linear")] PositionLinear = 2
    }
    private struct DottedEnumRequest { [JsonPropertyName("topic")] public TestDottedEnum Topic; }

    [Fact]
    public void Push_ShouldHandleEnum_With_JsonPropertyName()
    {
        // Arrange
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        var req = new DottedEnumRequest { Topic = TestDottedEnum.OrderLinear };

        // Act
        JsonPushulator.Push(req, writer);
        writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("order.linear", doc.RootElement.GetProperty("topic").GetString());

        // Test another value
        buffer.Clear();
        writer.Reset();
        req.Topic = TestDottedEnum.PositionLinear;
        JsonPushulator.Push(req, writer);
        writer.Flush();
        json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        using var doc2 = JsonDocument.Parse(json);
        Assert.Equal("position.linear", doc2.RootElement.GetProperty("topic").GetString());
    }

    [JsonAsArray]
    private struct PositionalItem
    {
        [JsonToString] public double Price;
        [JsonToString] public double Qty;
    }

    private struct DeepNestedRoot
    {
        [JsonPropertyName("name")] public string Name;
        [JsonPropertyName("info")] public MetaWithPositional Info;
    }

    private struct MetaWithPositional
    {
        [JsonPropertyName("ts"), JsonToMs] public DateTime Time;
        [JsonPropertyName("items")] public FastList<PositionalItem> Items;
    }

    [Fact]
    public void Push_ShouldHandleDeeplyNestedComplexStructures()
    {
        // Arrange
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        var ts = new DateTime(2026, 2, 18, 15, 0, 0, DateTimeKind.Utc);

        var root = new DeepNestedRoot
        {
            Name = "DeepTest",
            Info = new MetaWithPositional
            {
                Time = ts,
                Items = new FastList<PositionalItem>(2)
            }
        };
        root.Info.Items.Add(new PositionalItem { Price = 100.5, Qty = 1.0 });
        root.Info.Items.Add(new PositionalItem { Price = 200.0, Qty = 0.5 });

        // Act
        JsonPushulator.Push(root, writer);
        writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        using var doc = JsonDocument.Parse(json);
        var rootProp = doc.RootElement;

        Assert.Equal("DeepTest", rootProp.GetProperty("name").GetString());
        var info = rootProp.GetProperty("info");
        Assert.Equal(new DateTimeOffset(ts).ToUnixTimeMilliseconds(), info.GetProperty("ts").GetInt64());

        var items = info.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());

        // Item 0: ["100.5", "1"] - because of [JsonAsArray] and [JsonToString]
        Assert.Equal(JsonValueKind.Array, items[0].ValueKind);
        Assert.Equal("100.5", items[0][0].GetString());
        Assert.Equal("1", items[0][1].GetString());

        // Item 1: ["200", "0.5"]
        Assert.Equal("200", items[1][0].GetString());
        Assert.Equal("0.5", items[1][1].GetString());
    }

    [JsonAsArray]
    private struct DateTimePositionalArgs
    {
        [JsonToMs] public DateTime Expires;
        [JsonToStringMs] public DateTime ExpiresStr;
    }

    [Fact]
    public void Push_ShouldHandleDateTimeAttributes_In_JsonAsArray()
    {
        // Arrange
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        var ts = new DateTime(2026, 2, 18, 12, 0, 0, DateTimeKind.Utc);
        var args = new DateTimePositionalArgs { Expires = ts, ExpiresStr = ts };

        // Act
        JsonPushulator.Push(args, writer);
        writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var expectedMs = new DateTimeOffset(ts).ToUnixTimeMilliseconds();
        Assert.Equal(expectedMs, root[0].GetInt64());
        Assert.Equal(expectedMs.ToString(), root[1].GetString());
    }

    [Fact]
    public void Push_ShouldSerializeRootStructAsArray()
    {
        // Arrange
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        var args = new PositionalArgs
        {
            Key = new FixedString32("root-key"),
            Expires = 999,
            Signature = "root-sign"
        };

        // Act - Serializing a struct marked with [JsonAsArray] directly
        JsonPushulator.Push(args, writer);
        writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(3, doc.RootElement.GetArrayLength());
        Assert.Equal("root-key", doc.RootElement[0].GetString());
        Assert.Equal("999", doc.RootElement[1].GetString());
        Assert.Equal("root-sign", doc.RootElement[2].GetString());
    }

    private sealed class ConditionalModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("price")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double Price { get; set; }

        [JsonPropertyName("tag")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Tag { get; set; }

        [JsonPropertyName("active")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool? IsActive { get; set; }
    }

    [Fact]
    public void Push_ShouldOmitDefaultValues_WhenConditionIsSet()
    {
        // Arrange
        var model = new ConditionalModel
        {
            Id = 1,
            Name = "Test",
            Price = 0.0, // Should be omitted
            Tag = null,  // Should be omitted
            IsActive = null // Should be omitted
        };

        var buffer = new ArrayBufferWriter<byte>(1024);
        using var writer = new Utf8JsonWriter(buffer);

        // Act
        JsonPushulator.Push(model, writer);
        writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Contains("\"id\":1", json);
        Assert.Contains("\"name\":\"Test\"", json);
        Assert.DoesNotContain("\"price\"", json);
        Assert.DoesNotContain("\"tag\"", json);
        Assert.DoesNotContain("\"active\"", json);
    }

    [Fact]
    public void Push_ShouldIncludeNonDefaultValues_WhenConditionIsSet()
    {
        // Arrange
        var model = new ConditionalModel
        {
            Id = 1,
            Name = "Test",
            Price = 99.99, // Should be included
            Tag = "Important", // Should be included
            IsActive = true // Should be included
        };

        var buffer = new ArrayBufferWriter<byte>(1024);
        using var writer = new Utf8JsonWriter(buffer);

        // Act
        JsonPushulator.Push(model, writer);
        writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Contains("\"id\":1", json);
        Assert.Contains("\"price\":99.99", json);
        Assert.Contains("\"tag\":\"Important\"", json);
        Assert.Contains("\"active\":true", json);
    }
}
