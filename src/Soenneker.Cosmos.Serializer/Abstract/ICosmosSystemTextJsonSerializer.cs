using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Cosmos.Serializer.Abstract;

/// <summary>
/// Marks the Cosmos serializer that uses <c>System.Text.Json</c> and memory streams supplied by <see cref="IMemoryStreamUtil"/>.
/// </summary>
public interface ICosmosSystemTextJsonSerializer
{
}
