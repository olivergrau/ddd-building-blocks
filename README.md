# DDD Building Blocks

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

This repository contains a collection of .NET components that implement tactical Domain Driven Design (DDD) patterns.  The solution is split into several packages that can be used independently or together to speed up development of event sourced applications.

## Solution Structure

- **DDD.BuildingBlocks.Core** – interfaces and base classes for the aggregate root, domain events, repositories, command pattern and other utilities.
- **DDD.BuildingBlocks.DevelopmentPackage** – in-memory implementations useful for local development and testing.
- **DDD.BuildingBlocks.PostgreSQLPackage** – production event-store provider using Npgsql and explicit SQL without EF Core.
- **DDD.BuildingBlocks.MSSQLPackage** – production SQL Server event-store provider using Microsoft.Data.SqlClient, table-valued event batches, and explicit SQL without EF Core.
- **DDD.BuildingBlocks.AzurePackage** – Azure specific helpers such as Service Bus domain event handlers and blob storage support.
- **DDD.BuildingBlocks.DI.Extensions** – helpers for registering services with the .NET dependency injection container.
- **DDD.BuildingBlocks.Hosting.Background** – abstractions and helpers for implementing background worker services.
- **DDD.BuildingBlocks.Demo** – minimal project referencing all packages.
- **test** – unit and integration tests covering the building blocks.

For detailed documentation of the library itself refer to [`src/DDD.BuildingBlocks.Core/README.md`](src/DDD.BuildingBlocks.Core/README.md).

Version 2.0 is a breaking release. Existing consumers should read the complete [1.x to 2.0 change inventory](CHANGES.md) and the ordered [2.0 migration guide](MIGRATION-2.0.md). Packages are distributed as assets of the [GitHub releases](https://github.com/olivergrau/ddd-building-blocks/releases); they are not published to a public NuGet registry.

## Building and Testing

The solution targets .NET 10.0 LTS. The SDK version is pinned in `global.json`. You can build everything and run the full test suite using the .NET CLI:

```bash
# Build all projects
dotnet restore DDD.BuildingBlocks.sln
dotnet build DDD.BuildingBlocks.sln --no-restore

# Execute all unit and integration tests
dotnet test DDD.BuildingBlocks.sln --no-build

# Create the seven active framework packages from the Release build
dotnet build DDD.BuildingBlocks.sln --no-restore --configuration Release
dotnet pack DDD.BuildingBlocks.Packages.slnf --no-build --configuration Release --output artifacts/packages
```

The provider-contract suite runs unchanged against the in-memory reference provider, PostgreSQL, and SQL Server through Testcontainers.

Projection processing uses committed-feed positions with versioned checkpoints, transactional read-model callbacks, visible failure state, retry, and rebuild support. Checkpoint stores are included for In-Memory, PostgreSQL, and SQL Server.
