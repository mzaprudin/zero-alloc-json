using System.Buffers;
using System.Text;
using System.Text.Json.Serialization;
using GapSonar.Core.Shared;
using GapSonar.Core.Shared.Infrastructure.Serialization;

namespace GapSonar.TransportLab.Tests;

/// <summary>Verifies zero-alloc parsing of unquoted and quoted numeric types across segment boundaries.</summary>
public sealed class NumericBoundaryTests
{
    /// <summary>Every two-segment split of unquoted numeric values preserves exact values.</summary>
    [Fact]
    public void Populate_UnquotedNumbers_AllSegmentBoundaries_PreservesValues()
    {
        var bytes = "{\"Id\":42,\"Price\":123.45,\"Amount\":999.88,\"Count\":10,\"Symbol\":\"BTCUSDT\"}"u8.ToArray();
        const long EXPECTED_ID = 42;
        const double EXPECTED_PRICE = 123.45;
        const decimal EXPECTED_AMOUNT = 999.88m;
        const int EXPECTED_COUNT = 10;
        var interner = new SimpleStringInterner();
        StringInternerDelegate intern = interner.Intern;
        for (int split = 1; split < bytes.Length; split++)
        {
            var first = new Segment(bytes.AsMemory(0, split));
            var last = first.Append(bytes.AsMemory(split));
            var sequence = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
            var update = new NumericModel();
            JsonPopulator.Populate(sequence, update, intern);
            Assert.Equal(EXPECTED_ID, update.Id);
            Assert.Equal(EXPECTED_PRICE, update.Price);
            Assert.Equal(EXPECTED_AMOUNT, update.Amount);
            Assert.Equal(EXPECTED_COUNT, update.Count);
            Assert.Equal("BTCUSDT", update.Symbol);
        }
    }

    /// <summary>Verifies that parsing unquoted numbers across segment boundaries allocates zero bytes in the managed heap.</summary>
    [Fact]
    public void Populate_UnquotedNumbersAcrossSegments_ZeroAllocations()
    {
        var bytes = "{\"Id\":42,\"Price\":123.45,\"Amount\":999.88,\"Count\":10,\"Symbol\":\"BTCUSDT\"}"u8.ToArray();
        var interner = new SimpleStringInterner();
        StringInternerDelegate intern = interner.Intern;

        // Warm-up / JIT compiler
        var first = new Segment(bytes.AsMemory(0, 10));
        var last = first.Append(bytes.AsMemory(10));
        var sequence = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        var update = new NumericModel();
        JsonPopulator.Populate(sequence, update, intern);

        for (int split = 1; split < bytes.Length; split++)
        {
            var s1 = new Segment(bytes.AsMemory(0, split));
            var s2 = s1.Append(bytes.AsMemory(split));
            var seq = new ReadOnlySequence<byte>(s1, 0, s2, s2.Memory.Length);

            long before = GC.GetAllocatedBytesForCurrentThread();
            JsonPopulator.Populate(seq, update, intern);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.Equal(0, allocated);
        }
    }

    public sealed class NumericModel
    {
        public long Id { get; set; }
        public double Price { get; set; }
        public decimal Amount { get; set; }
        public int Count { get; set; }
        [JsonIntern]
        public string Symbol { get; set; } = string.Empty;
    }

    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory) => Memory = memory;
        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }
}
