using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GapSonar.Core.Shared.Infrastructure.Serialization;

namespace GapSonar.Core.Tests;

public class JsonPopulatorEnhancedTests
{
    public class ChildDto
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    public class ParentDto
    {
        public int Id { get; set; }
        public ChildDto Child { get; set; } = null!;
        public ChildDto? NullableChild { get; set; }
    }

    public struct ChildStruct
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class ParentWithStruct
    {
        public ChildStruct Point { get; set; }
    }

    [Fact]
    public void Populate_NestedObject_PopulatesCorrectly()
    {
        var json = """
        {
            "Id": 1,
            "Child": {
                "Name": "Nested",
                "Value": 99
            }
        }
        """;

        var target = new ParentDto { Child = new ChildDto() };
        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), target);

        Assert.Equal(1, target.Id);
        Assert.NotNull(target.Child);
        Assert.Equal("Nested", target.Child.Name);
        Assert.Equal(99, target.Child.Value);
    }

    [Fact]
    public void Populate_NestedObject_CreatesInstanceIfNull()
    {
        var json = """
        {
            "Id": 2,
            "Child": {
                "Name": "Created",
                "Value": 100
            }
        }
        """;

        var target = new ParentDto(); // Child is null
        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), target);

        Assert.Equal(2, target.Id);
        Assert.NotNull(target.Child);
        Assert.Equal("Created", target.Child.Name);
        Assert.Equal(100, target.Child.Value);
    }

    [Fact]
    public void Populate_NestedStruct_PopulatesCorrectly()
    {
        var json = """
        {
            "Point": {
                "X": 10,
                "Y": 20
            }
        }
        """;

        var target = new ParentWithStruct();
        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), target);

        Assert.Equal(10, target.Point.X);
        Assert.Equal(20, target.Point.Y);
    }

    [Fact]
    public void Populate_RootArray_ListInt()
    {
        var json = "[1, 2, 3]";
        var target = new List<int>();

        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), target);

        Assert.Equal(3, target.Count);
        Assert.Equal(1, target[0]);
        Assert.Equal(2, target[1]);
        Assert.Equal(3, target[2]);
    }

    public class SimpleItem
    {
        public int Id { get; set; }
    }

    [Fact]
    public void Populate_RootArray_ListObject()
    {
        var json = """[{"Id": 1}, {"Id": 2}]""";
        var target = new List<SimpleItem>();

        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), target);

        Assert.Equal(2, target.Count);
        Assert.Equal(1, target[0].Id);
        Assert.Equal(2, target[1].Id);
    }

    public class ReusableItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class FastListContainer
    {
        public FastList<ReusableItem> Items { get; } = new(10);
    }

    [Fact]
    public void Populate_FastList_ReusesObjects()
    {
        var json = """
        {
            "Items": [
                { "Id": 1, "Name": "First" },
                { "Id": 2, "Name": "Second" }
            ]
        }
        """;

        var container = new FastListContainer();

        // 1. First population
        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), container);

        Assert.Equal(2, container.Items.Count);
        Assert.Equal(1, container.Items[0].Id);
        Assert.Equal("First", container.Items[0].Name);

        var item1Ref = container.Items[0];

        // 2. Second population (different data, same size)
        var json2 = """
        {
            "Items": [
                { "Id": 3, "Name": "Third" },
                { "Id": 4, "Name": "Fourth" }
            ]
        }
        """;

        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json2), container);

        Assert.Equal(2, container.Items.Count);
        Assert.Equal(3, container.Items[0].Id);

        // Ensure object identity is same (reference reused)
        Assert.Same(item1Ref, container.Items[0]);
    }

    // ========== FixedString Tests ==========

    public class FixedStringDto
    {
        public FixedString8 Small { get; set; }
        public FixedString16 Medium { get; set; }
        public FixedString32 Large { get; set; }
        public FixedString64 ExtraLarge { get; set; }
        public FixedString128 Huge { get; set; }
    }

    [Fact]
    public void Populate_FixedString8_ParsesCorrectly()
    {
        var json = """{"Small": "test"}""";
        var target = new FixedStringDto();

        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), target);

        Assert.Equal("test", target.Small.ToString());
        Assert.Equal(4, target.Small.Length);
    }

    [Fact]
    public void Populate_FixedString16_ParsesCorrectly()
    {
        var json = """{"Medium": "hello world"}""";
        var target = new FixedStringDto();

        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), target);

        Assert.Equal("hello world", target.Medium.ToString());
        Assert.Equal(11, target.Medium.Length);
    }

    [Fact]
    public void Populate_FixedString32_ParsesCorrectly()
    {
        var json = """{"Large": "symbol name BTCUSDT"}""";
        var target = new FixedStringDto();

        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), target);

        Assert.Equal("symbol name BTCUSDT", target.Large.ToString());
    }

    [Fact]
    public void Populate_FixedString64_ParsesCorrectly()
    {
        var json = """{"ExtraLarge": "this is a longer message for testing"}""";
        var target = new FixedStringDto();

        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), target);

        Assert.Equal("this is a longer message for testing", target.ExtraLarge.ToString());
    }

    [Fact]
    public void Populate_FixedString128_ParsesCorrectly()
    {
        var longText = "This is a very long text that should fit in 128 bytes easily for testing purposes";
        var json = $$"""{"Huge": "{{longText}}"}""";
        var target = new FixedStringDto();

        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), target);

        Assert.Equal(longText, target.Huge.ToString());
    }

    [Fact]
    public void Populate_FixedString_TruncatesWhenTooLong()
    {
        // 8 byte capacity, but trying to set 20 bytes
        var json = """{"Small": "this is way too long"}""";
        var target = new FixedStringDto();

        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), target);

        // Should be truncated to 8 bytes
        Assert.Equal(8, target.Small.Length);
        Assert.Equal("this is ", target.Small.ToString());
    }

    [Fact]
    public void Populate_FixedString_EmptyString()
    {
        var json = """{"Small": ""}""";
        var target = new FixedStringDto();

        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), target);

        Assert.True(target.Small.IsEmpty);
        Assert.Equal(0, target.Small.Length);
    }

    [Fact]
    public void Populate_FixedString_MultipleFields()
    {
        var json = """
        {
            "Small": "BTC",
            "Medium": "BTCUSDT",
            "Large": "order_12345",
            "ExtraLarge": "some longer text here",
            "Huge": "even longer text for huge field"
        }
        """;
        var target = new FixedStringDto();

        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), target);

        Assert.Equal("BTC", target.Small.ToString());
        Assert.Equal("BTCUSDT", target.Medium.ToString());
        Assert.Equal("order_12345", target.Large.ToString());
        Assert.Equal("some longer text here", target.ExtraLarge.ToString());
        Assert.Equal("even longer text for huge field", target.Huge.ToString());
    }

    [Fact]
    public void Populate_FixedString_ReusesInstance()
    {
        var json1 = """{"Small": "first"}""";
        var json2 = """{"Small": "second"}""";
        var target = new FixedStringDto();

        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json1), target);
        Assert.Equal("first", target.Small.ToString());

        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json2), target);
        Assert.Equal("second", target.Small.ToString());
    }

    public struct FixedStringStruct
    {
        public FixedString32 Symbol { get; set; }
        public double Price { get; set; }
    }

    [Fact]
    public void Populate_FixedString_InStruct()
    {
        var json = """
        {
            "Symbol": "ETHUSDT",
            "Price": 1234.56
        }
        """;
        var target = new FixedStringStruct();

        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), ref target);

        Assert.Equal("ETHUSDT", target.Symbol.ToString());
        Assert.Equal(1234.56, target.Price);
    }
}
