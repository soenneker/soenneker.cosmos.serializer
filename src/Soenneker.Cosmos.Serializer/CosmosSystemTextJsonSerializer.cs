using Microsoft.Azure.Cosmos;
using Soenneker.Cosmos.Serializer.Abstract;
using Soenneker.Json.OptionsCollection;
using Soenneker.Utils.MemoryStream.Abstract;
using System;
using System.IO;
using System.Text.Json;

namespace Soenneker.Cosmos.Serializer;

public sealed class CosmosSystemTextJsonSerializer : CosmosSerializer, ICosmosSystemTextJsonSerializer
{
    private static readonly JsonSerializerOptions _options = JsonOptionsCollection.WebOptions;
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
            // Only bypass Read for the concrete BCL stream; subclasses may transform reads.
            if (stream.GetType() == typeof(MemoryStream))
            {
                var memory = (MemoryStream)stream;
                long remaining = memory.Length - memory.Position;
                if (remaining >= 0)
                {
                    if (memory.TryGetBuffer(out ArraySegment<byte> buffer))
                        return DeserializeBuffer<T>(buffer.AsSpan((int)memory.Position));

                    // Small opaque buffers can use the stack instead of the JSON stream reader's pooled buffer.
                    if (remaining <= 1024)
                    {
                        Span<byte> bufferCopy = stackalloc byte[(int)remaining];
                        memory.ReadExactly(bufferCopy);
                        return DeserializeBuffer<T>(bufferCopy);
                    }
                }
            }

            return JsonSerializer.Deserialize<T>(stream, _options)!;
        }
    }

    private static T DeserializeBuffer<T>(ReadOnlySpan<byte> json)
    {
        // Stream deserialization accepts a UTF-8 BOM.
        if (json.StartsWith("\uFEFF"u8))
            json = json[3..];
        return JsonSerializer.Deserialize<T>(json, _options)!;
    }

    public override Stream ToStream<T>(T input)
    {
        MemoryStream ms = _memoryStreamUtil.GetSync();

        try
        {
            JsonSerializer.Serialize(ms, input, _options);
            ms.Position = 0;
            return ms;
        }
        catch
        {
            ms.Dispose();
            throw;
        }
    }
}
