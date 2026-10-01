using System.Text.Json.Serialization;

namespace Soenneker.Cosmos.Serializer.Tests;

public class LinqSerializerBaseDocument
{
    [JsonPropertyName("base_alias")]
    public string? InheritedLabel { get; set; }
}
