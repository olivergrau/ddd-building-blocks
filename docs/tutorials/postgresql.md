# Use PostgreSQL

The PostgreSQL adapter uses Npgsql and explicit SQL. It includes event storage, projection checkpoints, snapshots, and versioned schema migration.

## Install

```bash
dotnet add package DDD.BuildingBlocks.PostgreSQLPackage --version 2.1.0
```

## Create the data source

Create one application-wide `NpgsqlDataSource`. Npgsql manages connection pooling through the data source.

```csharp
using Npgsql;

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
await using var dataSource = dataSourceBuilder.Build();
```

Register it as a singleton in hosted applications and dispose it during host shutdown.

## Choose a schema

```csharp
var options = new PostgreSqlEventStoreOptions
{
    Schema = "ddd_building_blocks"
};
```

Custom schema names must be unquoted lower-case PostgreSQL identifiers. Use the same options instance for all provider components.

## Apply migrations

```csharp
await new PostgreSqlEventStoreMigrator(dataSource, options)
    .MigrateAsync(cancellationToken);
```

The migrator is idempotent. In production, run it from a controlled deployment job with schema-change permissions, then run the application with narrower data permissions.

## Create the event store and repository

```csharp
IEventStoreProvider eventStore =
    new PostgreSqlEventStoreProvider(dataSource, options);

IEventCodec eventCodec = CreateApplicationEventCodec();

IEventSourcingRepository repository =
    new EventSourcingRepository(eventStore, eventCodec);
```

Register `NpgsqlDataSource`, `IEventStoreProvider`, `IEventCodec`, and `IEventSourcingRepository` as application-wide services. The repository itself has no per-request mutable state.

## Add checkpoint and snapshot stores

```csharp
IProjectionCheckpointStore checkpoints =
    new PostgreSqlProjectionCheckpointStore(dataSource, options);

ISnapshotStoreProvider snapshots =
    new PostgreSqlSnapshotStoreProvider(
        dataSource,
        snapshotFrequency: 100,
        options);
```

Snapshot use requires a snapshot codec and an aggregate implementing `ISnapshotEnabled`. Checkpoint use requires projections to execute read-model SQL through the supplied Npgsql connection and transaction.

## Health and startup checks

At startup or readiness time, verify:

- the database is reachable;
- the expected migration version exists;
- application credentials can read/append events and use checkpoints;
- stable event registrations cover all event types the deployment may encounter.

Do not append a test event to a production stream as a health check.

## Backup and restore

Use PostgreSQL-native `pg_dump`/`pg_restore` or your managed service's consistent backup mechanism. A recovery test must include event tables, stream rows, position allocator, snapshots, projection checkpoints, and application read models.

After restore, compare stream counts and versions and rebuild a disposable projection from position zero.
