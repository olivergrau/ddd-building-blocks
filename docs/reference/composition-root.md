# Composition-root checklist

The composition root is responsible for making durable contract choices explicit. Avoid hiding these registrations in domain projects.

## Required registrations

### Event contracts

- one stable `EventTypeRegistry` covering every event this deployment may read or write;
- all sequential upcasters for historical schemas;
- one configured `IEventCodec`;
- consistent JSON serializer options.

RocketLaunch centralizes this in `RocketLaunchEventCodec.Create()`.

### Persistence

- one `IEventStoreProvider` implementation;
- one `IEventSourcingRepository` using that provider and codec;
- provider migrator executed before application traffic;
- provider options shared by event, checkpoint, and snapshot components.

### Dispatch

- call `AddDddBuildingBlocksDispatching` with every assembly containing commands/handlers/subscribers;
- register handler dependencies with correct lifetimes;
- resolve `ICommandDispatcher`/`IDomainEventNotifier` rather than handlers through a locator.

## Optional registrations

### Snapshots

Configure all or none:

- `SnapshotTypeRegistry`;
- `ISnapshotCodec`;
- `ISnapshotStoreProvider`;
- aggregates implementing `ISnapshotEnabled`.

### Projections

- `IProjectionCheckpointStore` matching the event-store provider;
- projection implementations with stable keys;
- a `ProjectionRunner` or worker that runs bounded batches;
- read-model services capable of using the supplied transaction context.

### External publication

- post-commit/outbox mechanism;
- integration-event mapping;
- delivery retry and idempotency policy;
- correlation/causation propagation.

## PostgreSQL composition sketch

```csharp
var options = new PostgreSqlEventStoreOptions();
var dataSource = NpgsqlDataSource.Create(connectionString);

await new PostgreSqlEventStoreMigrator(dataSource, options)
    .MigrateAsync(cancellationToken);

services.AddSingleton(dataSource);
services.AddSingleton(options);
services.AddSingleton<IEventCodec>(_ => CreateApplicationEventCodec());
services.AddSingleton<IEventStoreProvider>(provider =>
    new PostgreSqlEventStoreProvider(
        provider.GetRequiredService<NpgsqlDataSource>(),
        provider.GetRequiredService<PostgreSqlEventStoreOptions>()));
services.AddSingleton<IProjectionCheckpointStore>(provider =>
    new PostgreSqlProjectionCheckpointStore(
        provider.GetRequiredService<NpgsqlDataSource>(),
        provider.GetRequiredService<PostgreSqlEventStoreOptions>()));
services.AddSingleton<IEventSourcingRepository>(provider =>
    new EventSourcingRepository(
        provider.GetRequiredService<IEventStoreProvider>(),
        provider.GetRequiredService<IEventCodec>()));
```

Production applications should run migration outside normal host startup where operational policy requires separated credentials.

## Startup validation checklist

- all event/snapshot keys are unique;
- current class versions match registry schema versions;
- upcaster chains are contiguous;
- every command has exactly one handler;
- provider schema is current;
- cancellation tokens reach hosted workers and database calls;
- projection keys are stable and versions intentional;
- no 1.x package/provider remains in dependency graphs.
