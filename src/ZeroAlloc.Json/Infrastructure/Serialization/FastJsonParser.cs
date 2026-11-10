using System.Text.Json;

namespace GapSonar.Core.Shared.Infrastructure.Serialization;

public static class FastJsonParser
{
    public static void Skip(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.PropertyName)
        {
            reader.Read();
        }

        if (reader.TokenType == JsonTokenType.StartObject || reader.TokenType == JsonTokenType.StartArray)
        {
            reader.Skip();
        }
    }
}
