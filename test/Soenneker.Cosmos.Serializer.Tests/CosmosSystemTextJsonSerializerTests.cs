using Soenneker.Tests.HostedUnit;

namespace Soenneker.Cosmos.Serializer.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class CosmosSystemTextJsonSerializerTests : HostedUnitTest
{
    public CosmosSystemTextJsonSerializerTests(Host host) : base(host)
    {

    }

    [Test]
    public void Default()
    {

    }
}
