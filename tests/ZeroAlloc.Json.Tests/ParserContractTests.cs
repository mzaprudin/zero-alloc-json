using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GapSonar.Core.Shared.Infrastructure.Serialization;

namespace GapSonar.TransportLab.Tests;

/// <summary>Locks down extraction fixes, segmented inputs, and reuse semantics.</summary>
public sealed class ParserContractTests
{
    /// <summary>Escaped JSON text must be decoded before it is interned.</summary>
    [Theory]
    [InlineData("hello\\nworld", "hello\nworld")]
    [InlineData("BTC\\u0055SDT", "BTCUSDT")]
    [InlineData("quote\\\"slash\\\\", "quote\"slash\\")]
    public void Populate_EscapedInternedString_DecodesAndReusesReference(string encoded, string expected)
    {
        var bytes = Encoding.UTF8.GetBytes("{\"Symbol\":\"" + encoded + "\"}");
        var interner = new SimpleStringInterner();
        StringInternerDelegate intern = interner.Intern;
        var first = new Update();
        var second = new Update();
        JsonPopulator.Populate(bytes, first, intern);
        JsonPopulator.Populate(bytes, second, intern);
        Assert.Equal(expected, first.Symbol);
        Assert.Same(first.Symbol, second.Symbol);
    }

    /// <summary>Every two-segment split of a representative update gives identical results.</summary>
    [Fact]
    public void Populate_AllSegmentBoundaries_PreservesValues()
    {
        var bytes = "{\"Id\":42,\"Price\":\"123.45\",\"Symbol\":\"BTCUSDT\"}"u8.ToArray();
        const long EXPECTED_ID = 42;
        const double EXPECTED_PRICE = 123.45;
        var interner = new SimpleStringInterner();
        StringInternerDelegate intern = interner.Intern;
        for (int split = 1; split < bytes.Length; split++)
        {
            var first = new Segment(bytes.AsMemory(0, split));
            var last = first.Append(bytes.AsMemory(split));
            var sequence = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
            var update = new Update();
            JsonPopulator.Populate(sequence, update, intern);
            Assert.Equal(EXPECTED_ID, update.Id);
            Assert.Equal(EXPECTED_PRICE, update.Price);
            Assert.Equal("BTCUSDT", update.Symbol);
        }
    }

    /// <summary>Reuse intentionally implements partial updates, retaining absent members.</summary>
    [Fact]
    public void Populate_MissingMemberOnReusedObject_RetainsPreviousValue()
    {
        const long PREVIOUS_ID = 42;
        const double NEW_PRICE = 123.45;
        var update = new Update { Id = PREVIOUS_ID, Symbol = "BTCUSDT" };
        JsonPopulator.Populate("{\"Price\":123.45}"u8, update);
        Assert.Equal(PREVIOUS_ID, update.Id);
        Assert.Equal(NEW_PRICE, update.Price);
        Assert.Equal("BTCUSDT", update.Symbol);
    }

    /// <summary>Unknown nested values do not prevent subsequent known fields from being parsed.</summary>
    [Fact]
    public void Populate_UnknownNestedValue_ParsesFollowingMember()
    {
        const long EXPECTED_ID = 42;
        var update = new Update();
        JsonPopulator.Populate("{\"unknown\":{\"items\":[1,2]},\"Id\":42}"u8, update);
        Assert.Equal(EXPECTED_ID, update.Id);
    }

    /// <summary>Malformed JSON does not pass as a complete valid update.</summary>
    [Theory]
    [InlineData("{\"Id\":")]
    [InlineData("{\"Id\":42,}")]
    public void Populate_MalformedDocument_ThrowsJsonException(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        Assert.ThrowsAny<JsonException>(() => JsonPopulator.Populate(bytes, new Update()));
    }

    /// <summary>Documents permissive legacy conversion for a malformed quoted number.</summary>
    [Fact]
    public void Populate_InvalidQuotedNumber_ReturnsZero()
    {
        var update = new Update();
        JsonPopulator.Populate("{\"Price\":\"invalid\"}"u8, update);
        Assert.Equal(0, update.Price);
    }

    /// <summary>Representative primitive and interned members.</summary>
    public sealed class Update
    {
        /// <summary>Trade identifier.</summary>
        public long Id { get; set; }
        /// <summary>Price supporting quoted numeric input.</summary>
        public double Price { get; set; }
        /// <summary>Optional bounded interned symbol.</summary>
        [JsonIntern]
        public string Symbol { get; set; } = string.Empty;
    }

    /// <summary>Builds explicitly segmented input without network nondeterminism.</summary>
    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        /// <summary>Initializes one segment.</summary>
        public Segment(ReadOnlyMemory<byte> memory) => Memory = memory;
        /// <summary>Links the next segment with its absolute running offset.</summary>
        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }
}
