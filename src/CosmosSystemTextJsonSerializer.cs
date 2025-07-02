using System;
using System.IO;
using System.Threading;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using Soenneker.Cosmos.Serializer.Abstract;
using Soenneker.Extensions.Stream;
using Soenneker.Json.OptionsCollection;
using Soenneker.Reflection.Cache;
using Soenneker.Reflection.Cache.Types;
using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Cosmos.Serializer;

///<inheritdoc cref="ICosmosSystemTextJsonSerializer"/>
public sealed class CosmosSystemTextJsonSerializer : CosmosSerializer, ICosmosSystemTextJsonSerializer
{
    private readonly JsonObjectSerializer _systemTextJsonSerializer;
    private readonly IMemoryStreamUtil _memoryStreamUtil;

    private readonly bool _cachingEnabled;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly ReflectionCache? _reflectionCache;
    private readonly CachedType? _cachedStreamType;
    private readonly Type? _streamType;

    public CosmosSystemTextJsonSerializer(IMemoryStreamUtil memoryStreamUtil, bool cachingEnabled = true)
    {
        _systemTextJsonSerializer = new JsonObjectSerializer(JsonOptionsCollection.WebOptions); // TODO: get more strict for performance
        _memoryStreamUtil = memoryStreamUtil;

        _cachingEnabled = cachingEnabled;

        if (_cachingEnabled)
        {
            _reflectionCache = new ReflectionCache();
            _cachedStreamType = _reflectionCache.GetCachedType(typeof(Stream));
        }
        else
        {
            _streamType = typeof(Stream);
        }
    }

    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            if (stream is {CanSeek: true, Length: 0})
            {
                return default!;
            }

            Type typeOfT = typeof(T);

            if (_cachingEnabled)
            {
                if (_cachedStreamType!.IsAssignableFrom(typeOfT))
                {
                    return (T)(object)stream;
                }
            }
            else
            {
                if (_streamType!.IsAssignableFrom(typeOfT))
                {
                    return (T)(object)stream;
                }
            }

            return (T) _systemTextJsonSerializer.Deserialize(stream, typeOfT, CancellationToken.None)!; // TODO: cancellationToken
        }
    }

    public override Stream ToStream<T>(T input)
    {
        MemoryStream streamPayload = _memoryStreamUtil.GetSync();
        _systemTextJsonSerializer.Serialize(streamPayload, input, typeof(T), CancellationToken.None); // TODO: cancellationToken
        streamPayload.ToStart(); // Seek because Position set directly will lose the buffer: https://stackoverflow.com/a/71596118
        return streamPayload;
    }
}