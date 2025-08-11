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
        // If caller requested the raw stream, hand it off and DO NOT dispose it here.
        if (_streamType.IsAssignableFrom(typeof(T)))
            return (T)(object)stream;

        // Empty-body fast path (safe only if seekable)
        if (stream is {CanSeek: true, Length: 0})
        {
            stream.Dispose();
            return default!;
        }

        try
        {
            return (T)_serializer.Deserialize(stream, typeof(T), CancellationToken.None)!;
        }
        finally
        {
            stream.Dispose();
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