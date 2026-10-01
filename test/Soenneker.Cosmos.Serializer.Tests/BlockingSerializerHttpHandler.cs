using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Cosmos.Serializer.Tests;

public sealed class BlockingSerializerHttpHandler : HttpMessageHandler
{
    public int Calls { get; private set; }
    public int DocumentRequests { get; private set; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        if (request.RequestUri?.AbsolutePath.Contains("/dbs/", StringComparison.Ordinal) == true)
            DocumentRequests++;
        throw new InvalidOperationException("Offline query translation must never contact Cosmos.");
    }
}
