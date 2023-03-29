using System.IO;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using Soenneker.Extensions.Stream;
using Soenneker.Json.OptionsCollection;
using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Cosmos.Serializer;

/// <summary>
/// Uses <see cref="JsonObjectSerializer"/> which leverages Systems.Text.Json providing a simple API to interact with on the Azure SDKs.
/// </summary>
public class CosmosSystemTextJsonSerializer : CosmosSerializer
{
    private readonly JsonObjectSerializer _systemTextJsonSerializer;
    private readonly IMemoryStreamUtil _memoryStreamUtil;

    public CosmosSystemTextJsonSerializer(IMemoryStreamUtil memoryStreamUtil)
    {
        _systemTextJsonSerializer = new JsonObjectSerializer(JsonOptionsCollection.WebOptions); // TODO: get more strict for performance
        _memoryStreamUtil = memoryStreamUtil;
    }

    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            if (stream.CanSeek && stream.Length == 0)
            {
                return default!;
            }

            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T) (object) stream;
            }

            return (T) _systemTextJsonSerializer.Deserialize(stream, typeof(T), default)!; // TODO: cancellationToken
        }
    }

    public override Stream ToStream<T>(T input)
    {
        MemoryStream streamPayload = _memoryStreamUtil.GetSync();
        _systemTextJsonSerializer.Serialize(streamPayload, input, typeof(T), default); // TODO: cancellationToken
        streamPayload.ToStart(); // Seek because Position set directly will lose the buffer: https://stackoverflow.com/a/71596118
        return streamPayload;
    }
}