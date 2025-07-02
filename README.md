# DDD Building Blocks

This repository contains a collection of .NET components that implement tactical Domain Driven Design (DDD) patterns.  The solution is split into several packages that can be used independently or together to speed up development of event sourced applications.

## Solution Structure

- **DDD.BuildingBlocks.Core** – interfaces and base classes for the aggregate root, domain events, repositories, command pattern and other utilities.
- **DDD.BuildingBlocks.DevelopmentPackage** – in-memory implementations useful for local development and testing.
- **DDD.BuildingBlocks.MSSQLPackage** – provider and service implementations backed by Microsoft SQL Server (see [`src/DDD.BuildingBlocks.MSSQLPackage/README.md`](src/DDD.BuildingBlocks.MSSQLPackage/README.md)).
- **DDD.BuildingBlocks.AzurePackage** – Azure specific helpers such as Service Bus domain event handlers and blob storage support.
- **DDD.BuildingBlocks.DI.Extensions** – helpers for registering services with the .NET dependency injection container.
- **DDD.BuildingBlocks.Hosting.Background** – abstractions and helpers for implementing background worker services.
- **DDD.BuildingBlocks.Demo** – minimal project referencing all packages.
- **test** – unit and integration tests covering the building blocks.

For detailed documentation of the library itself refer to [`src/DDD.BuildingBlocks.Core/README.md`](src/DDD.BuildingBlocks.Core/README.md).

## Building and Testing

The solution targets .NET 9.0.  You can build everything and run the full test suite using the .NET CLI:

```bash
# Build all projects
 dotnet build DDD.BuildingBlocks.sln

# Execute all unit and integration tests
 dotnet test DDD.BuildingBlocks.sln
```

Integration tests that rely on SQL Server use Testcontainers to start a temporary database instance.
