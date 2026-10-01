using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Cosmos.Serializer.Tests;

public sealed class LinqSerializerDocument : LinqSerializerBaseDocument
{
    public string? EntityType { get; set; }
    public LinqSerializerState State { get; set; }
    [JsonInclude]
    [JsonPropertyName("field_alias")]
    public int IncludedNumber;
    public string? ExcludedField;
    public string? OriginalTransactionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    [JsonPropertyName("account_key")]
    public string? AccountId { get; set; }
    public LinqSerializerContact? Contact { get; set; }
    public string? id { get; set; }
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}
