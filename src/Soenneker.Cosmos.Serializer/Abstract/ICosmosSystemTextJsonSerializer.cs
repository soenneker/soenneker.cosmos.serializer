using Microsoft.IO;
using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Cosmos.Serializer.Abstract
{
    /// <summary>
    /// A fast, lightweight JSON (de)serializer for Azure Cosmos DB <para/>
    /// This serializer leverages Systems.Text.Json, overriding the default Json.Net serializer. It also uses <see cref="RecyclableMemoryStream"/> (via <see cref="IMemoryStreamUtil"/>) for further memory improvements.
    /// </summary>
    public interface ICosmosSystemTextJsonSerializer
    {
    }
}