[![](https://img.shields.io/nuget/v/Soenneker.Cosmos.Serializer.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Cosmos.Serializer/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.serializer/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.serializer/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Cosmos.Serializer.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Cosmos.Serializer/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.serializer/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.serializer/actions/workflows/codeql.yml)

# Soenneker.Cosmos.Serializer

A fast, lightweight JSON (de)serializer for Azure Cosmos DB This serializer leverages Systems.Text.Json, overriding the default Json.Net serializer. It also uses `RecyclableMemoryStream` (via `IMemoryStreamUtil`) for further memory improvements.

## Install

```bash
dotnet add package Soenneker.Cosmos.Serializer
```

## What you get

- `ICosmosSystemTextJsonSerializer` — A fast, lightweight JSON (de)serializer for Azure Cosmos DB This serializer leverages Systems.Text.Json, overriding the default Json.Net serializer. It also uses `RecyclableMemoryStream` (via `IMemoryStreamUtil`) for further memory improvements.
