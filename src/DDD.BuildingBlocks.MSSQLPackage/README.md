# DDD.BuildingBlocks SQL Server Provider

This package implements `IEventStoreProvider` for Microsoft SQL Server with Microsoft.Data.SqlClient and explicit parameterized SQL. It is a clean-room parity implementation of the current envelope-based contracts; it does not reuse the removed CLR-type-bound persistence format. EF Core is intentionally not used in the event-store append or read path.

Apply the schema migration during deployment or startup and then construct the provider with the same connection string and options:

```csharp
var options = new SqlServerEventStoreOptions();

await new SqlServerEventStoreMigrator(connectionString, options)
    .MigrateAsync(cancellationToken);

IEventStoreProvider provider = new SqlServerEventStoreProvider(connectionString, options);
```

The default schema is `ddd_building_blocks`. A custom schema must be a regular SQL Server identifier.

The provider uses `UPDLOCK` and `HOLDLOCK` for atomic stream concurrency and a table-valued parameter for set-based event-batch insertion. Global positions are allocated through a transactional singleton row so committed-feed checkpoints cannot skip a later-committing transaction with a lower position.

Snapshot storage and projection checkpoints remain separate persistence contracts and are not hidden inside the event-store append transaction. Snapshot activation is optional, but this package must provide its snapshot implementation in F9.
