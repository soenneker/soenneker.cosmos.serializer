using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using Soenneker.Cosmos.Serializer.Abstract;
using Soenneker.Extensions.Stream;
using Soenneker.Json.OptionsCollection;
using Soenneker.Utils.MemoryStream.Abstract;
using System;
using System.IO;
using System.Threading;

namespace Soenneker.Cosmos.Serializer;

///<inheritdoc cref="ICosmosSystemTextJsonSerializer"/>
public sealed class CosmosSystemTextJsonSerializer : CosmosSerializer, ICosmosSystemTextJsonSerializer
{
    private static readonly JsonObjectSerializer _serializer = new(JsonOptionsCollection.WebOptions);
    private static readonly Type _streamType = typeof(Stream);

    private readonly IMemoryStreamUtil _memoryStreamUtil;

    public CosmosSystemTextJsonSerializer(IMemoryStreamUtil memoryStreamUtil)
    {
        _memoryStreamUtil = memoryStreamUtil;
    }

    public override T FromStream<T>(Stream stream)
    {
        if (typeof(T) == _streamType)
            return (T)(object)stream;

        if (stream is MemoryStream { Length: 0 })
        {
            stream.Dispose();
            return default!;
        }

        using (stream)
        {
            return (T)_serializer.Deserialize(stream, typeof(T), CancellationToken.None)!;
        }
    }

    public override Stream ToStream<T>(T input)
    {
        MemoryStream ms = _memoryStreamUtil.GetSync();
        _serializer.Serialize(ms, input, typeof(T), CancellationToken.None);
        ms.ToStart();
        return ms;
    }
}