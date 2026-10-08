using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Soenneker.Utils.MemoryStream;
using System.Threading;

namespace Soenneker.Cosmos.Serializer.Tests;

public sealed class LinqSerializerTests
{
    [Test]
    public async Task Member_names_follow_the_same_STJ_contract_as_document_storage(CancellationToken cancellationToken)
    {
        await using var util = new MemoryStreamUtil();
        var serializer = new CosmosSystemTextJsonSerializer(util);
        serializer.Should().BeAssignableTo<CosmosLinqSerializer>();
        serializer.SerializeMemberName(typeof(LinqSerializerDocument).GetProperty(nameof(LinqSerializerDocument.EntityType))!).Should().Be("entityType");
        serializer.SerializeMemberName(typeof(LinqSerializerDocument).GetProperty(nameof(LinqSerializerDocument.AccountId))!).Should().Be("account_key");
        serializer.SerializeMemberName(typeof(LinqSerializerContact).GetProperty(nameof(LinqSerializerContact.PhoneNumber))!).Should().Be("phone_number");
        serializer.SerializeMemberName(typeof(LinqSerializerDocument).GetProperty(nameof(LinqSerializerDocument.InheritedLabel))!).Should().Be("base_alias");
        serializer.SerializeMemberName(typeof(LinqSerializerDocument).GetProperty(nameof(LinqSerializerDocument.id))!).Should().Be("id");
        serializer.SerializeMemberName(typeof(LinqSerializerDocument).GetProperty(nameof(LinqSerializerDocument.Extra))!).Should().BeNull();
    }

    [Test]
    public async Task Adding_LINQ_support_preserves_existing_document_JSON_and_round_trip(CancellationToken cancellationToken)
    {
        await using var util = new MemoryStreamUtil();
        var serializer = new CosmosSystemTextJsonSerializer(util);
        var input = new LinqSerializerDocument
        {
            EntityType = "Wallet", OriginalTransactionId = "transaction", AccountId = "organization",
            InheritedLabel = "inherited", id = "document", Contact = new() { PhoneNumber = "+13125550100" },
            Extra = new() { ["risk"] = JsonSerializer.SerializeToElement(7) }
        };
        using var stream = serializer.ToStream(input);
        using var json = JsonDocument.Parse(stream);
        var stored = json.RootElement;
        stored.GetProperty("entityType").GetString().Should().Be("Wallet");
        stored.GetProperty("originalTransactionId").GetString().Should().Be("transaction");
        stored.GetProperty("account_key").GetString().Should().Be("organization");
        stored.GetProperty("base_alias").GetString().Should().Be("inherited");
        stored.GetProperty("contact").GetProperty("phone_number").GetString().Should().Be("+13125550100");
        stored.GetProperty("id").GetString().Should().Be("document");
        stored.GetProperty("risk").GetInt32().Should().Be(7);
        stored.TryGetProperty("extra", out _).Should().BeFalse();
        stored.TryGetProperty("EntityType", out _).Should().BeFalse();
        var roundTrip = serializer.FromStream<LinqSerializerDocument>(serializer.ToStream(input));
        roundTrip.AccountId.Should().Be(input.AccountId);
        roundTrip.Contact!.PhoneNumber.Should().Be(input.Contact.PhoneNumber);
        roundTrip.Extra!["risk"].GetInt32().Should().Be(7);
    }

    [Test]
    public async Task Offline_SDK_queries_use_camel_case_aliases_nested_and_inherited_names(CancellationToken cancellationToken)
    {
        await using var util = new MemoryStreamUtil();
        using var handler = new BlockingSerializerHttpHandler();
        using var httpClient = new System.Net.Http.HttpClient(handler);
        using var client = new CosmosClient("https://localhost:8081", Convert.ToBase64String(new byte[64]),
            new CosmosClientOptions { Serializer = new CosmosSystemTextJsonSerializer(util),
                ConnectionMode = ConnectionMode.Gateway, HttpClientFactory = () => httpClient });
        var query = client.GetContainer("offline", "documents").GetItemLinqQueryable<LinqSerializerDocument>()
            .Where(document => document.EntityType == "Wallet" && document.OriginalTransactionId == "transaction" &&
                document.AccountId == "organization" && document.Contact!.PhoneNumber == "+13125550100" &&
                document.InheritedLabel == "inherited" && document.id == "document")
            .OrderByDescending(document => document.CreatedAt).ToQueryDefinition().QueryText;
        foreach (string name in new[] { "entityType", "originalTransactionId", "account_key", "contact", "phone_number", "base_alias", "id", "createdAt" })
            query.Should().Contain($"[\"{name}\"]");
        query.Should().NotContain("[\"EntityType\"]");
        // SDK construction may probe account metadata in the background; the handler blocks every request.
        handler.DocumentRequests.Should().Be(0);
    }

    [Test]
    public async Task Offline_SDK_scalar_enum_and_date_literals_match_stored_STJ_values(CancellationToken cancellationToken)
    {
        await using var util = new MemoryStreamUtil();
        var serializer = new CosmosSystemTextJsonSerializer(util);
        DateTimeOffset cutoff = new(2026, 10, 1, 12, 30, 0, TimeSpan.FromHours(6));
        using var stored = JsonDocument.Parse(serializer.ToStream(new LinqSerializerDocument
        {
            State = LinqSerializerState.Ready, CreatedAt = cutoff
        }));
        using var handler = new BlockingSerializerHttpHandler();
        using var httpClient = new System.Net.Http.HttpClient(handler);
        using var client = new CosmosClient("https://localhost:8081", Convert.ToBase64String(new byte[64]),
            new CosmosClientOptions { Serializer = serializer, ConnectionMode = ConnectionMode.Gateway,
                HttpClientFactory = () => httpClient });
        string query = client.GetContainer("offline", "documents").GetItemLinqQueryable<LinqSerializerDocument>()
            .Where(document => document.State == LinqSerializerState.Ready && document.CreatedAt >= cutoff)
            .ToQueryDefinition().QueryText;
        query.Should().Contain("[\"state\"]");
        query.Should().Contain("[\"createdAt\"]");
        query.Should().Contain(stored.RootElement.GetProperty("state").GetRawText());
        query.Should().Contain(stored.RootElement.GetProperty("createdAt").GetRawText());
        handler.DocumentRequests.Should().Be(0);
    }

    [Test]
    public async Task Explicitly_included_field_alias_matches_storage_and_SQL_with_other_fields_excluded(CancellationToken cancellationToken)
    {
        await using var util = new MemoryStreamUtil();
        var serializer = new CosmosSystemTextJsonSerializer(util);
        using var stored = JsonDocument.Parse(serializer.ToStream(new LinqSerializerDocument
        {
            IncludedNumber = 42, ExcludedField = "must not be stored"
        }));
        stored.RootElement.GetProperty("field_alias").GetInt32().Should().Be(42);
        stored.RootElement.TryGetProperty("includedNumber", out _).Should().BeFalse();
        stored.RootElement.TryGetProperty("excludedField", out _).Should().BeFalse();
        serializer.SerializeMemberName(typeof(LinqSerializerDocument).GetField(nameof(LinqSerializerDocument.IncludedNumber))!)
            .Should().Be("field_alias");
        var roundTrip = serializer.FromStream<LinqSerializerDocument>(serializer.ToStream(new LinqSerializerDocument { IncludedNumber = 42 }));
        roundTrip.IncludedNumber.Should().Be(42);
        using var handler = new BlockingSerializerHttpHandler();
        using var httpClient = new System.Net.Http.HttpClient(handler);
        using var client = new CosmosClient("https://localhost:8081", Convert.ToBase64String(new byte[64]),
            new CosmosClientOptions { Serializer = serializer, ConnectionMode = ConnectionMode.Gateway,
                HttpClientFactory = () => httpClient });
        string query = client.GetContainer("offline", "documents").GetItemLinqQueryable<LinqSerializerDocument>()
            .Where(document => document.IncludedNumber == 42).ToQueryDefinition().QueryText;
        query.Should().Contain("[\"field_alias\"]");
        query.Should().NotContain("[\"IncludedNumber\"]");
        handler.DocumentRequests.Should().Be(0);
    }

    [Test]
    public async Task Offline_SDK_flattens_extension_data_member_paths(CancellationToken cancellationToken)
    {
        await using var util = new MemoryStreamUtil();
        using var handler = new BlockingSerializerHttpHandler();
        using var httpClient = new System.Net.Http.HttpClient(handler);
        using var client = new CosmosClient("https://localhost:8081", Convert.ToBase64String(new byte[64]),
            new CosmosClientOptions { Serializer = new CosmosSystemTextJsonSerializer(util),
                ConnectionMode = ConnectionMode.Gateway, HttpClientFactory = () => httpClient });
        string query = client.GetContainer("offline", "documents").GetItemLinqQueryable<LinqSerializerDocument>()
            .Select(document => document.Extra!["risk"]).ToQueryDefinition().QueryText;
        query.Should().Contain("[\"risk\"]");
        query.Should().NotContain("[\"extra\"]");
        query.Should().NotContain("[\"Extra\"]");
        // SDK construction may probe account metadata in the background; the handler blocks every request.
        handler.DocumentRequests.Should().Be(0);
    }
}
