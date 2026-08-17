# Migrating from DDD.BuildingBlocks 1.x to 2.0

This guide describes a safe migration sequence. DDD.BuildingBlocks 2.0 intentionally breaks persistence and dispatch contracts; perform the upgrade as one tested release rather than incrementally mixing 1.x and 2.0 runtime components.

## 1. Establish a recoverable baseline

1. Stop writes or establish a consistent export boundary.
2. Back up the 1.x event and snapshot stores using the database-native backup mechanism.
3. Record event counts, stream counts, final stream versions, and representative aggregate states.
4. Verify that the 1.x application can rebuild its read models from the backed-up history.
5. Keep the 1.x application and database available as the rollback unit until the 2.0 validation is complete.

## 2. Upgrade the application platform

- Install the .NET 10 SDK.
- Target `net10.0`.
- Update every `DDD.BuildingBlocks.*` reference to exactly `2.0.0`.
- Enable nullable analysis and fix warnings before changing persistence behavior where practical.

Do not reference a 1.x provider package from a 2.0 Core application.

## 3. Register stable event contracts

Create one composition-root registry for all persisted events. Keys become durable storage contracts and must not be renamed when CLR namespaces or class names change.

```csharp
var eventTypes = new EventTypeRegistry()
    .Register<OrderCreated>("orders.order-created", currentSchemaVersion: 1)
    .Register<OrderRenamed>("orders.order-renamed", currentSchemaVersion: 2);

IEventCodec eventCodec = new SystemTextJsonEventCodec(eventTypes);
```

For every historical event shape, add sequential upcasters until the current schema can deserialize it. Preserve historical JSON fixtures in consumer tests.

## 4. Migrate repository and cancellation usage

Replace `IEventStorageProvider` implementations with `IEventStoreProvider`. Construct or register the repository with an event store and codec:

```csharp
var repository = new EventSourcingRepository(eventStore, eventCodec);
var order = await repository.GetByIdAsync<Order, OrderId>(id, cancellationToken);
await repository.SaveAsync(order!, cancellationToken);
```

Pass a meaningful `CancellationToken` through commands, handlers, repositories, providers, projections, and hosted services. Do not convert cancellation into an application failure.

Use `RaiseEvent` in aggregate behavior. Existing `ApplyEvent` calls still compile as a compatibility alias, but should not be used for new code.

## 5. Replace locator-based dispatch

Remove all use of `ServiceLocator`, `IDependencyResolver`, and `DefaultCommandProcessor`. Register handlers through the 2.0 DI extensions at startup. Ensure every command has exactly one handler and that scoped handler dependencies are registered with the correct lifetime.

## 6. Deploy a new event-store schema

Choose either `DDD.BuildingBlocks.PostgreSQLPackage` or `DDD.BuildingBlocks.MSSQLPackage`. Run its idempotent migrator before starting 2.0 application writes.

The new schema stores stable aggregate/event keys, JSON payloads, `BIGINT` stream versions, event IDs, timestamps, and global positions. It is deliberately not an in-place interpretation of the 1.x SQL Server schema.

For legacy data, write a one-off migration tool that:

1. reads each 1.x event in stream order;
2. maps its CLR type identity to the new stable event key;
3. converts the payload into the registered JSON schema;
4. assigns the correct zero-based `long` stream version;
5. preserves event identity, commit time, correlation metadata, and aggregate identity where available;
6. appends the transformed stream through a controlled import path;
7. compares per-stream counts, versions, and reconstructed aggregate state.

Never copy binary CLR payloads directly into the new JSON envelope columns.

## 7. Recreate snapshots

Treat all 1.x snapshots as disposable. The safest migration is to discard them, replay migrated events, and let 2.0 create new snapshots.

When enabling snapshots, register stable snapshot keys and configure the store and codec together:

```csharp
var snapshotTypes = new SnapshotTypeRegistry()
    .Register<OrderSnapshot>("orders.order-snapshot", currentSchemaVersion: 1);

ISnapshotCodec snapshotCodec = new SystemTextJsonSnapshotCodec(snapshotTypes);
ISnapshotStoreProvider snapshotStore = /* provider-specific implementation */;

var repository = new EventSourcingRepository(
    eventStore,
    eventCodec,
    snapshotStore,
    snapshotCodec);
```

Prove that full replay and snapshot-plus-residual replay produce the same state.

## 8. Migrate projections

- Give each projection a stable name and positive version.
- Create its provider checkpoint store.
- Apply the read-model change and checkpoint in the same transaction.
- Rebuild from global position zero in a disposable environment.
- Compare rebuilt data with the expected 1.x read model and representative API responses.
- Test poison-event visibility, retry, and independent progress of other projections.

## 9. Validate before cutover

The release is ready for a consumer only after all of the following pass:

- historical event fixtures decode and upcast;
- every migrated stream has the expected event count and final version;
- representative and boundary aggregates reconstruct identically;
- optimistic concurrency rejects conflicting writers;
- projections rebuild deterministically;
- backup/restore succeeds for the new provider;
- snapshot failures do not affect successful event commits;
- cancellation reaches database operations;
- no 1.x package or schema dependency remains.

## 10. Cut over and rollback

Deploy schema migration before application startup, then switch all writers together. Monitor event append failures, concurrency conflicts, projection lag/failure state, and database capacity.

Rollback means stopping 2.0 writers and returning to the complete 1.x application/database baseline. Do not point the 1.x application at the 2.0 schema or attempt to merge histories without an explicit reconciliation procedure.
