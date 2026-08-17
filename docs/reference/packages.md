# Package catalog

All packages in the 2.0 release target .NET 10 and must be kept on the same version.

## DDD.BuildingBlocks.Core

Use in domain and application projects.

Provides:

- `Entity`, `EntityId`, `ValueObject`, `AggregateRoot`;
- domain events, envelopes, registries, codec, and upcasters;
- repository and event-store contracts;
- snapshot contracts and default JSON codec;
- projection runner/checkpoint contracts;
- command and domain-event dispatch contracts;
- error classification and framework exceptions.

Core has no PostgreSQL, SQL Server, ASP.NET Core, or provider dependency.

## DDD.BuildingBlocks.DevelopmentPackage

Use for tests, samples, and local development.

Provides:

- `InMemoryEventStoreProvider`;
- `InMemorySnapshotStoreProvider`;
- `InMemoryProjectionCheckpointStore`;
- local publishing helpers and background projection support.

Do not use in-memory persistence as a production durability mechanism.

## DDD.BuildingBlocks.PostgreSQLPackage

Use in PostgreSQL infrastructure/composition projects.

Provides:

- `PostgreSqlEventStoreMigrator`;
- `PostgreSqlEventStoreProvider`;
- `PostgreSqlProjectionCheckpointStore`;
- `PostgreSqlSnapshotStoreProvider`;
- validated schema options.

Uses Npgsql and explicit parameterized SQL.

## DDD.BuildingBlocks.MSSQLPackage

Use in SQL Server infrastructure/composition projects.

Provides:

- `SqlServerEventStoreMigrator`;
- `SqlServerEventStoreProvider`;
- `SqlServerProjectionCheckpointStore`;
- `SqlServerSnapshotStoreProvider`;
- validated schema options.

Uses Microsoft.Data.SqlClient, explicit locking SQL, and table-valued event batches.

## DDD.BuildingBlocks.DI.Extensions

Use at the Microsoft dependency-injection composition root.

Provides:

- validated assembly scanning for command handlers and event subscribers;
- `ScopedCommandDispatcher`;
- `ScopedDomainEventNotifier`;
- module and service-registration helpers.

## DDD.BuildingBlocks.Hosting.Background

Use in worker/host projects that need framework background-service helpers.

Provides:

- background task queue abstractions;
- queued and timed hosted services;
- background worker interfaces and options.

This package does not decide projection correctness or event publication semantics.

## DDD.BuildingBlocks.AzurePackage

Use only in Azure infrastructure projects.

Provides Azure Service Bus event senders and Azure Blob string storage helpers. Treat integration messages as separate external contracts from persisted domain events.

## Typical dependency direction

```text
Domain -> Core
Application -> Domain + Core
Infrastructure.PostgreSQL -> Application + PostgreSQLPackage
Infrastructure.SqlServer -> Application + MSSQLPackage
Host -> Application + Infrastructure + DI.Extensions + optional Hosting.Background/AzurePackage
Tests -> Domain/Application + DevelopmentPackage
```
