# Use SQL Server

The SQL Server adapter uses Microsoft.Data.SqlClient, explicit locking SQL, and table-valued event batches. It matches the PostgreSQL provider contract without sharing its schema.

## Install

```bash
dotnet add package DDD.BuildingBlocks.MSSQLPackage --version 2.0.0
```

## Configure the schema

```csharp
var options = new SqlServerEventStoreOptions
{
    Schema = "ddd_building_blocks"
};
```

Custom schema names must be regular unquoted SQL Server identifiers.

## Apply migrations

```csharp
await new SqlServerEventStoreMigrator(connectionString, options)
    .MigrateAsync(cancellationToken);
```

Run migration as a privileged deployment task. The 2.0 migrator creates the new envelope-based schema; it does not reinterpret the removed 1.x CLR-serialized schema.

## Create the event store

```csharp
IEventStoreProvider eventStore =
    new SqlServerEventStoreProvider(connectionString, options);

IEventCodec eventCodec = CreateApplicationEventCodec();

IEventSourcingRepository repository =
    new EventSourcingRepository(eventStore, eventCodec);
```

The provider opens pooled SqlClient connections per operation. Configure pooling, encryption, authentication, and transient connection behavior in the connection string and deployment environment.

## Add checkpoints and snapshots

```csharp
IProjectionCheckpointStore checkpoints =
    new SqlServerProjectionCheckpointStore(connectionString, options);

ISnapshotStoreProvider snapshots =
    new SqlServerSnapshotStoreProvider(
        connectionString,
        snapshotFrequency: 100,
        options);
```

Projection SQL must use the `SqlConnection` and `SqlTransaction` supplied by `ProjectionTransactionContext`; opening another connection breaks atomic checkpointing.

## Concurrency behavior

The provider locks the stream row with `UPDLOCK` and `HOLDLOCK`, validates expected version, and inserts an event batch through a table-valued parameter. Do not add application-side “check then insert” logic around the provider; it would reintroduce a race.

## Backup and restore

Use native SQL Server backups or the equivalent managed-service mechanism. Restore tests should validate:

- all stream/event rows and final versions;
- global committed-feed positions;
- projection checkpoint status;
- snapshot readability or safe fallback;
- deterministic projection rebuild.

For an upgrade from the old provider, follow the [1.x migration guide](../guides/upgrading-from-1.x.md).
