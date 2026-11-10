using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GapSonar.Core.Shared.Infrastructure.Serialization;

namespace GapSonar.Core.Tests;

public class JsonPopulatorTests
{
    public enum Status
    {
        None = 0,
        Active = 1,
        Inactive = 2
    }

    public class ComplexDto
    {
        public int Id { get; set; }
        public long Timestamp { get; set; }
        public double Price { get; set; }
        public decimal Amount { get; set; }
        public bool IsValid { get; set; }
        public string Name { get; set; } = "";

        [JsonPropertyName("desc")]
        public string Description { get; set; } = "";

        public DateTime CreatedAt { get; set; }
        public Status Status { get; set; }

        // Fields not in JSON should remain default
        public int IgnoredField { get; set; } = -1;
    }

    public struct ComplexStruct
    {
        public int Id { get; set; }
        public double Price { get; set; }
        public Status Status { get; set; }
    }

    [Fact]
    public void Populate_Class_AllFields_FromStandardSerializer()
    {
        // 1. Create Source Object
        var source = new ComplexDto
        {
            Id = 123,
            Timestamp = 1678900000000,
            Price = 123.456,
            Amount = 789.012m,
            IsValid = true,
            Name = "TestName",
            Description = "Test Description",
            CreatedAt = DateTime.UtcNow,
            Status = Status.Active,
            IgnoredField = -1 // Not serialized usually unless we force it, but here it matches target default
        };

        // 2. Serialize to Bytes using Standard Serializer
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(source);

        // 3. Populate Target
        var target = new ComplexDto();
        JsonPopulator.Populate(jsonBytes, target);

        // 4. Assert
        Assert.Equal(source.Id, target.Id);
        Assert.Equal(source.Timestamp, target.Timestamp);
        Assert.Equal(source.Price, target.Price);
        Assert.Equal(source.Amount, target.Amount);
        Assert.Equal(source.IsValid, target.IsValid);
        Assert.Equal(source.Name, target.Name);
        Assert.Equal(source.Description, target.Description);
        // Json serialization usually loses some precision or format, but standard serializer <-> parser should match for ISO8601
        // Note: JsonSerializer defaults to ISO8601 strings for DateTime.
        // JsonPopulator supports ISO8601 via global::GapSonar.Core.Shared.Shared.ParseHighPrecisionTimestamp (if implemented to support it)
        // OR via ParserHelpers.ParseDateTime which calls global::GapSonar.Core.Shared.Shared.ParseHighPrecisionTimestamp for strings.
        // Standard JsonSerializer outputs "yyyy-MM-ddTHH:mm:ss.fffffffZ".
        // global::GapSonar.Core.Shared.Shared.ParseHighPrecisionTimestamp needs to handle this.
        // Let's verify if standard serializer output works.
        Assert.Equal(source.CreatedAt, target.CreatedAt);
        Assert.Equal(source.Status, target.Status);
        Assert.Equal(-1, target.IgnoredField);
    }

    [Fact]
    public void Populate_Struct_AllFields_FromStandardSerializer()
    {
        var source = new ComplexStruct
        {
            Id = 456,
            Price = 99.99,
            Status = Status.Inactive
        };

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(source);

        var target = new ComplexStruct();
        JsonPopulator.Populate(jsonBytes, ref target);

        Assert.Equal(source.Id, target.Id);
        Assert.Equal(source.Price, target.Price);
        Assert.Equal(source.Status, target.Status);
    }

    [Fact]
    public void Populate_Handles_Manual_StringNumbers()
    {
        // Standard serializer doesn't serialize numbers as strings by default.
        // So we keep one manual JSON test for this specific Exchange API scenario.
        var json = """
        {
            "Id": "789",
            "Price": "0.0001",
            "IsValid": "true",
            "Status": "1"
        }
        """;

        var bytes = Encoding.UTF8.GetBytes(json);
        var dto = new ComplexDto();

        JsonPopulator.Populate(bytes, dto);

        Assert.Equal(789, dto.Id);
        Assert.Equal(0.0001, dto.Price, 5);
        Assert.True(dto.IsValid);
        Assert.Equal(Status.Active, dto.Status);
    }

    [Fact]
    public void Populate_Handles_UnixTimestamp_As_Number()
    {
        // Standard JsonSerializer serializes DateTime as ISO string by default.
        // Exchanges often send Unix ms (number).
        var json = """
        {
            "CreatedAt": 1678900000000
        }
        """;

        var bytes = Encoding.UTF8.GetBytes(json);
        var dto = new ComplexDto();

        JsonPopulator.Populate(bytes, dto);

        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1678900000000).UtcDateTime, dto.CreatedAt);
    }

    [Theory]
    [InlineData("""{"CreatedAt":253402300800000}""")]
    [InlineData("""{"CreatedAt":"253402300800000"}""")]
    [InlineData("""{"CreatedAt":"not-a-date"}""")]
    public void Populate_DateTime_InvalidOrOutOfRange_ReturnsDefault(string json)
    {
        var dto = new ComplexDto();

        var exception = Record.Exception(() => JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), dto));

        Assert.Null(exception);
        Assert.Equal(default, dto.CreatedAt);
    }

    [Fact]
    public void Populate_Handles_Enum_Strings_CaseInsensitive()
    {
        var json = """
        {
            "Status": "active"
        }
        """;

        var bytes = Encoding.UTF8.GetBytes(json);
        var dto = new ComplexDto();

        JsonPopulator.Populate(bytes, dto);

        Assert.Equal(Status.Active, dto.Status);
    }

    [Fact]
    public void Populate_Handles_Enum_Strings_Exact()
    {
        var json = """
        {
            "Status": "Inactive"
        }
        """;

        var bytes = Encoding.UTF8.GetBytes(json);
        var dto = new ComplexDto();

        JsonPopulator.Populate(bytes, dto);

        Assert.Equal(Status.Inactive, dto.Status);
    }

    [Fact]
    public void Populate_Handles_Enum_Numbers()
    {
        var json = """
        {
            "Status": 2
        }
        """;

        var bytes = Encoding.UTF8.GetBytes(json);
        var dto = new ComplexDto();

        JsonPopulator.Populate(bytes, dto);

        Assert.Equal(Status.Inactive, dto.Status);
    }

    public enum TestDottedEnum
    {
        Unknown = 0,
        [JsonPropertyName("order.create")] OrderCreate = 1,
        [JsonPropertyName("position.linear")] PositionLinear = 2
    }

    public class EnumDto { public TestDottedEnum Op { get; set; } }

    [Fact]
    public void Populate_Handles_Enum_With_JsonPropertyName()
    {
        // Test with dotted name
        var json = "{\"Op\": \"order.create\"}";
        var dto = new EnumDto();
        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), dto);
        Assert.Equal(TestDottedEnum.OrderCreate, dto.Op);

        // Test with another dotted name
        json = "{\"Op\": \"position.linear\"}";
        dto = new EnumDto();
        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), dto);
        Assert.Equal(TestDottedEnum.PositionLinear, dto.Op);

        // Test with number
        json = "{\"Op\": 2}";
        dto = new EnumDto();
        JsonPopulator.Populate(Encoding.UTF8.GetBytes(json), dto);
        Assert.Equal(TestDottedEnum.PositionLinear, dto.Op);
    }

    public struct Nested1
    {
        public string Name { get; set; }
        public double Value { get; set; }
    }

    public struct Root1
    {
        public string Channel { get; set; }
        public Nested1 Result { get; set; }
    }

    [Fact]
    public void Populate_MismatchNestedStructure_ShouldNotCrash()
    {
        // JSON has "Result" with "Status" property (Nested2 structure)
        var json = """
        {
            "Channel": "test",
            "Result": { "Status": "success" }
        }
        """;

        var bytes = Encoding.UTF8.GetBytes(json);
        var target = new Root1();

        // Target.Result is Nested1 (has Name and Value, but no Status)
        JsonPopulator.Populate(bytes, ref target);

        Assert.Equal("test", target.Channel);
        Assert.Null(target.Result.Name);
        Assert.Equal(0, target.Result.Value);
    }
}
