[![](https://img.shields.io/nuget/v/Soenneker.Cosmos.Serializer.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Cosmos.Serializer/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.serializer/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.serializer/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Cosmos.Serializer.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Cosmos.Serializer/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.serializer/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.serializer/actions/workflows/codeql.yml)

# Soenneker.Cosmos.Serializer

An Azure Cosmos DB `CosmosSerializer` backed by `System.Text.Json` and pooled memory streams.

## Installation

```bash
dotnet add package Soenneker.Cosmos.Serializer
```

## Use with `CosmosClient`

```csharp
using Soenneker.Cosmos.Serializer;
using Soenneker.Utils.MemoryStream.Abstract;
using Soenneker.Utils.MemoryStream.Registrars;

services.AddMemoryStreamUtilAsSingleton();

IMemoryStreamUtil memoryStreams =
    serviceProvider.GetRequiredService<IMemoryStreamUtil>();

var serializer = new CosmosSystemTextJsonSerializer(memoryStreams);

var client = new CosmosClient(
    endpoint,
    accountKey,
    new CosmosClientOptions
    {
        Serializer = serializer
    });
```

`Soenneker.Cosmos.Client` already constructs and installs this serializer, so applications using that client utility do not need to configure it again.

## JSON behavior

Serialization uses `Soenneker.Json.OptionsCollection.JsonOptionsCollection.WebOptions`. The same options are used for reads and writes, keeping property naming and configured converters consistent. This replaces the Cosmos SDK's default Newtonsoft.Json serializer, so verify stored JSON compatibility before switching an existing container.

## Stream ownership

`ToStream` returns a positioned stream owned by the Cosmos SDK or direct caller. The recipient must dispose it. If serialization fails before the stream is returned, the serializer disposes the pooled stream itself.

`FromStream<T>` consumes and disposes the supplied stream after deserialization. The exception is `FromStream<Stream>`, which returns the original stream without disposing it and transfers ownership to the caller. An empty `MemoryStream` deserializes to `default(T)`.

Serialization and deserialization failures propagate to the caller. The adapter uses the synchronous `CosmosSerializer` contract, so it does not accept a cancellation token.
