# Enable snapshots

Snapshots are appropriate only after measuring a replay bottleneck. This tutorial assumes an existing event-sourced aggregate and provider.

## Implement a snapshot type

```csharp
public sealed class CounterSnapshot(
    string serializedAggregateId,
    long version,
    int value)
    : Snapshot(serializedAggregateId, version, "Counter")
{
    public int Value { get; } = value;
}
```

The snapshot must be JSON-serializable by your configured `JsonSerializerOptions`.

## Implement `ISnapshotEnabled`

```csharp
public sealed class Counter : AggregateRoot<CounterId>, ISnapshotEnabled
{
    // Aggregate behavior and event handlers omitted.

    public Snapshot TakeSnapshot() =>
        new CounterSnapshot(Id.ToString(), CurrentVersion, Value);

    public void ApplySnapshot(Snapshot snapshot)
    {
        var counter = snapshot as CounterSnapshot
            ?? throw new ArgumentException("Unexpected snapshot type.", nameof(snapshot));

        Id = new CounterId(counter.SerializedAggregateId);
        Value = counter.Value;
        CurrentVersion = counter.Version;
        LastCommittedVersion = counter.Version;
    }
}
```

Validate the concrete snapshot type. Restore every aggregate field needed before residual events apply, including version state.

RocketLaunch's `Mission` and `MissionSnapshot` provide a complete example with value objects, optional relations, and lifecycle state.

## Register the snapshot contract

```csharp
var snapshotRegistry = new SnapshotTypeRegistry()
    .Register<CounterSnapshot>("counter.snapshot", currentSchemaVersion: 1);

ISnapshotCodec snapshotCodec =
    new SystemTextJsonSnapshotCodec(snapshotRegistry);
```

The stable key follows the same durability rules as event keys.

Register the aggregate independently from its snapshot contract:

```csharp
var aggregateTypes = new AggregateTypeRegistry()
    .Register<Counter>("counter.counter");
```

## Select a store

For local development:

```csharp
ISnapshotStoreProvider snapshotStore =
    new InMemorySnapshotStoreProvider(snapshotFrequency: 100);
```

For production, use `PostgreSqlSnapshotStoreProvider` or `SqlServerSnapshotStoreProvider` after applying provider migrations.

## Enable repository snapshot support

```csharp
var repository = EventSourcingRepository.Create(
    eventStore,
    eventCodec,
    aggregateTypes,
    snapshotStore,
    snapshotCodec);
```

Store and codec must both be present. The repository writes snapshots after successful event commits when a frequency boundary is crossed.

## Prove equivalence

Write an integration test that:

1. creates a long event history;
2. loads it through a repository with snapshots enabled;
3. loads the same stream through a repository without snapshot support;
4. compares all domain-observable state and versions;
5. corrupts or version-invalidates the snapshot and proves full replay still succeeds;
6. forces snapshot writes to fail and proves event commit still succeeds.

Do not enable snapshots in production without this equivalence test.
