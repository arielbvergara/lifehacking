using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Configuration;

public static class JsonSerializerOptionsDefaults
{
    public static readonly JsonSerializerOptions DatabaseStorage = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
