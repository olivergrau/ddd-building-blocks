# Build a recoverable projection

This tutorial outlines a relational projection that counts events by stable event key. Use provider-specific SQL, but keep projection identity and runner behavior provider-neutral.

## Define projection identity

```csharp
public sealed class EventTypeCountProjection : IEventProjection
{
    public ProjectionKey Key { get; } = new("event-type-count", version: 1);

    public async Task ApplyAsync(
        EventEnvelope envelope,
        ProjectionTransactionContext context,
        CancellationToken cancellationToken)
    {
        // Provider-specific implementation shown below.
    }
}
```

The name and version are durable checkpoint identity. Increment the version for an incompatible read-model interpretation.

## Apply PostgreSQL SQL in the supplied transaction

```csharp
var connection = (NpgsqlConnection)context.Connection!;
var transaction = (NpgsqlTransaction)context.Transaction!;

await using var command = new NpgsqlCommand("""
    INSERT INTO read_model.event_type_counts (event_type, event_count)
    VALUES ($1, 1)
    ON CONFLICT (event_type) DO UPDATE
    SET event_count = read_model.event_type_counts.event_count + 1;
    """, connection, transaction);

command.Parameters.AddWithValue(envelope.EventType);
await command.ExecuteNonQueryAsync(cancellationToken);
```

For SQL Server, cast to `SqlConnection`/`SqlTransaction` and use a parameterized `MERGE` alternative or locked update/insert appropriate for the read model.

Never open a separate connection inside `ApplyAsync`. The read-model write and checkpoint must share the store-owned transaction.

## Run bounded batches

```csharp
var runner = new ProjectionRunner(eventStore, checkpointStore);
var projection = new EventTypeCountProjection();

ProjectionRunResult result;
do
{
    result = await runner.RunBatchAsync(
        projection,
        batchSize: 500,
        cancellationToken);
}
while (result.MayHaveMore);
```

In a background worker, pause between empty runs and pass the host stopping token. Expose the checkpoint position and failure state as operational metrics.

## Handle a poison event

If `ApplyAsync` throws, the transaction rolls back and the runner marks the projection faulted at that global position. Do not advance the checkpoint manually.

1. inspect the stored error and envelope;
2. fix code, schema, or data;
3. deploy the fix;
4. call `RetryBatchAsync`;
5. confirm the same position commits and failure state clears.

## Rebuild

```csharp
await runner.RebuildAsync(
    projection,
    async (context, token) =>
    {
        var connection = (NpgsqlConnection)context.Connection!;
        var transaction = (NpgsqlTransaction)context.Transaction!;
        await using var command = new NpgsqlCommand(
            "TRUNCATE TABLE read_model.event_type_counts;",
            connection,
            transaction);
        await command.ExecuteNonQueryAsync(token);
    },
    cancellationToken);
```

After reset, run batches from position zero. Compare the rebuilt result against an independently calculated expected result before switching consumers.
