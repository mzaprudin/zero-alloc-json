using System.Text;
using GapSonar.Core.Shared.Infrastructure.Serialization;
using Xunit;

namespace GapSonar.Core.Tests;

public class JsonPopulatorGuidTests
{
    public class GuidDto
    {
        public Guid Id { get; set; }
        public List<Guid> Tags { get; set; } = new();
    }

    [Fact]
    public void Populate_Handles_Guid_String()
    {
        var guid = Guid.Parse("7f8703ee-5cc5-4d03-94b6-02baf15a91fa");
        var json = $"{{\"Id\": \"{guid}\"}}";
        var bytes = Encoding.UTF8.GetBytes(json);
        var dto = new GuidDto();

        JsonPopulator.Populate(bytes, dto);

        Assert.Equal(guid, dto.Id);
        Assert.NotEqual(Guid.Empty, dto.Id);
    }

    [Fact]
    public void Populate_Handles_Guid_List()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var json = $"{{\"Tags\": [\"{guid1}\", \"{guid2}\"]}}";
        var bytes = Encoding.UTF8.GetBytes(json);
        var dto = new GuidDto();

        JsonPopulator.Populate(bytes, dto);

        Assert.Equal(2, dto.Tags.Count);
        Assert.Equal(guid1, dto.Tags[0]);
        Assert.Equal(guid2, dto.Tags[1]);
    }

    [Fact]
    public void Populate_Handles_Invalid_Guid_String()
    {
        var json = "{\"Id\": \"invalid-guid\"}";
        var bytes = Encoding.UTF8.GetBytes(json);
        var dto = new GuidDto();

        JsonPopulator.Populate(bytes, dto);

        Assert.Equal(Guid.Empty, dto.Id);
    }
}
