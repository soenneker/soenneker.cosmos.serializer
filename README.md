[![](https://img.shields.io/nuget/v/Soenneker.Cosmos.Serializer.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Cosmos.Serializer/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.serializer/publish.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.serializer/actions/workflows/publish.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Cosmos.Serializer.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Cosmos.Serializer/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Cosmos.Serializer
### A fast, lightweight JSON (de)serializer for Azure Cosmos DB

## Installation

```
Install-Package Soenneker.Cosmos.Serializer
```

## Usage

When constructing a `CosmosClientOptions`, simply set the `Serializer` property to a new instance and use your [`IMemoryStreamUtil`](https://github.com/soenneker/soenneker.utils.memorystream) singleton.

```csharp
var clientOptions = new CosmosClientOptions()
{
    Serializer = new CosmosSystemTextJsonSerializer(_memoryStreamUtil)
};
```