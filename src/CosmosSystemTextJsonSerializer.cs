using System;
using System.IO;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.IO;
using Soenneker.Extensions.Stream;
using Soenneker.Json.OptionsCollection;
using Soenneker.Reflection.Cache;
using Soenneker.Reflection.Cache.Types;
using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Cosmos.Serializer;

/// <summary>
/// A fast, lightweight JSON (de)serializer for Azure Cosmos DB <para/>
/// This serializer leverages Systems.Text.Json, overriding the default Json.Net serializer. It also uses <see cref="RecyclableMemoryStream"/> (via <see cref="IMemoryStreamUtil"/>) for further memory improvements.
/// </summary>
public class CosmosSystemTextJsonSerializer : CosmosSerializer
{
    private readonly JsonObjectSerializer _systemTextJsonSerializer;
    private readonly IMemoryStreamUtil _memoryStreamUtil;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly ReflectionCache _reflectionCache;

    private readonly CachedType _cachedStreamType;

    public CosmosSystemTextJsonSerializer(IMemoryStreamUtil memoryStreamUtil)
    {
        _systemTextJsonSerializer = new JsonObjectSerializer(JsonOptionsCollection.WebOptions); // TODO: get more strict for performance
        _memoryStreamUtil = memoryStreamUtil;
        _reflectionCache = new ReflectionCache();

        _cachedStreamType = _reflectionCache.GetCachedType(typeof(Stream));
    }

    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            if (stream.CanSeek && stream.Length == 0)
            {
                return default!;
            }

            Type typeOfT = typeof(T);

            if (_cachedStreamType.IsAssignableFrom(typeOfT))
            {
                return (T) (object) stream;
            }

            return (T) _systemTextJsonSerializer.Deserialize(stream, typeOfT, default)!; // TODO: cancellationToken
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