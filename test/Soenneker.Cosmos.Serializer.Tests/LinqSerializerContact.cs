using System.Text.Json.Serialization;

namespace Soenneker.Cosmos.Serializer.Tests;

public sealed class LinqSerializerContact
{
    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; }
}
