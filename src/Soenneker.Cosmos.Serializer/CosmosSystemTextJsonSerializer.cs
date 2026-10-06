using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Azure.Cosmos;
using Soenneker.Cosmos.Serializer.Abstract;
using Soenneker.Json.OptionsCollection;
using Soenneker.Utils.MemoryStream.Abstract;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Cosmos.Serializer;

public sealed class CosmosSystemTextJsonSerializer : CosmosLinqSerializer, ICosmosSystemTextJsonSerializer
{
    private readonly JsonSerializerOptions _options;
    private static readonly Type _streamType = typeof(Stream);

    private readonly IMemoryStreamUtil _memoryStreamUtil;

    [RequiresUnreferencedCode("The legacy serializer uses reflection. Supply a generated JsonSerializerContext instead.")]
    [RequiresDynamicCode("The legacy serializer may require runtime code generation. Supply a generated JsonSerializerContext instead.")]
    public CosmosSystemTextJsonSerializer(IMemoryStreamUtil memoryStreamUtil)
    {
        _options = JsonOptionsCollection.WebOptions;
        _memoryStreamUtil = memoryStreamUtil;
    }

    public CosmosSystemTextJsonSerializer(IMemoryStreamUtil memoryStreamUtil, JsonSerializerContext jsonContext)
    {
        ArgumentNullException.ThrowIfNull(jsonContext);
        _options = jsonContext.Options;
        _memoryStreamUtil = memoryStreamUtil;
    }

    private JsonTypeInfo<T> Contract<T>() => (JsonTypeInfo<T>)_options.GetTypeInfo(typeof(T));

    public override string SerializeMemberName(MemberInfo memberInfo)
    {
        ArgumentNullException.ThrowIfNull(memberInfo);
        // Extension data is flattened into the containing JSON object by STJ and the Cosmos LINQ translator.
        if (memberInfo.GetCustomAttribute<JsonExtensionDataAttribute>(inherit: true) != null)
            return null!;

        JsonPropertyNameAttribute? explicitName = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>(inherit: true);
        if (explicitName != null)
            return explicitName.Name;

        return _options.PropertyNamingPolicy?.ConvertName(memberInfo.Name) ?? memberInfo.Name;
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

            return JsonSerializer.Deserialize(stream, Contract<T>())!;
        }
    }

    private T DeserializeBuffer<T>(ReadOnlySpan<byte> json)
    {
        // Stream deserialization accepts a UTF-8 BOM.
        if (json.StartsWith("\uFEFF"u8))
            json = json[3..];
        return JsonSerializer.Deserialize(json, Contract<T>())!;
    }

    public override Stream ToStream<T>(T input)
    {
        MemoryStream ms = _memoryStreamUtil.GetSync();

        try
        {
            JsonSerializer.Serialize(ms, input, Contract<T>());
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
