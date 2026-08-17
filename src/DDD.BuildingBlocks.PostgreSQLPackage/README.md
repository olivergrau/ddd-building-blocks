# DDD.BuildingBlocks PostgreSQL Provider

This package implements `IEventStoreProvider` with Npgsql and explicit SQL. EF Core is intentionally not used in the event-store append or read path.

Create one application-wide `NpgsqlDataSource`, apply the schema migration during deployment or startup, and share the data source with the provider:

```csharp
var dataSource = NpgsqlDataSource.Create(connectionString);
var options = new PostgreSqlEventStoreOptions();

await new PostgreSqlEventStoreMigrator(dataSource, options)
    .MigrateAsync(cancellationToken);

IEventStoreProvider provider = new PostgreSqlEventStoreProvider(dataSource, options);
```

The default schema is `ddd_building_blocks`. A custom schema must be a lower-case unquoted PostgreSQL identifier.

The provider guarantees atomic expected-version append, unique event IDs, ordered stream reads, and a checkpoint-safe committed feed. Global feed positions are allocated under a short transaction-level serialization boundary because PostgreSQL sequences do not represent commit order.

Snapshot storage and projection checkpoints are separate persistence contracts and are not hidden inside the event-store append transaction. Snapshot activation is optional, but this package must provide its snapshot implementation in F9.
