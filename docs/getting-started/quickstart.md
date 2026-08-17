# Quickstart: your first event-sourced aggregate

This example creates a small `Counter` aggregate, commits two events, and loads the aggregate from its event stream.

## 1. Define a strongly typed identifier

Aggregate identifiers inherit from `EntityId<TSelf>`. Their string representation is the durable stream identifier, so it must be stable and reversible.

```csharp
using DDD.BuildingBlocks.Core.Domain;

public sealed class CounterId : EntityId<CounterId>
{
    [System.Text.Json.Serialization.JsonConstructor]
    public CounterId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Counter ID cannot be empty.");
        Value = value;
    }

    public CounterId(string value) : this(Guid.Parse(value)) { }

    public Guid Value { get; }

    protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString("D");
}
```

## 2. Define domain events

`TargetVersion` identifies the aggregate version before the event is applied. A creation event targets `-1`; the first applied event becomes stream version `0`.

```csharp
using DDD.BuildingBlocks.Core.Event;

public sealed class CounterCreated : DomainEvent
{
    public CounterCreated(CounterId counterId)
        : base(counterId.ToString(), targetVersion: -1, classVersion: 1)
    {
        CounterId = counterId;
    }

    public CounterId CounterId { get; }
}

public sealed class CounterIncremented : DomainEvent
{
    public CounterIncremented(CounterId counterId, int amount, long targetVersion)
        : base(counterId.ToString(), targetVersion, classVersion: 1)
    {
        CounterId = counterId;
        Amount = amount;
    }

    public CounterId CounterId { get; }
    public int Amount { get; }
}
```

Event classes are immutable historical facts. Do not rename persisted event identities merely because a CLR class or namespace changes.

## 3. Implement the aggregate

Business methods validate intent and call `RaiseEvent`. Private handlers marked with `InternalEventHandler` are the only place that changes persisted aggregate state.

```csharp
using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Domain;

public sealed class Counter : AggregateRoot<CounterId>
{
    public Counter(CounterId id) : base(id)
    {
        RaiseEvent(new CounterCreated(id));
    }

    // Required by repository rehydration.
    public Counter() : base(default!) { }

    public int Value { get; private set; }

    public void Increment(int amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        RaiseEvent(new CounterIncremented(Id, amount, CurrentVersion));
    }

    [InternalEventHandler]
    private void On(CounterCreated _) => Value = 0;

    [InternalEventHandler]
    private void On(CounterIncremented @event) => Value += @event.Amount;

    protected override CounterId GetIdFromStringRepresentation(string value) => new(value);
}
```

Never call event handlers directly. `RaiseEvent` checks stream identity and target version, invokes the matching handler, increments `CurrentVersion`, and records the event as uncommitted. Replay invokes the same handler without creating a new uncommitted event.

## 4. Register stable event keys

Every persisted event requires an explicit stable key:

```csharp
using DDD.BuildingBlocks.Core.Event.Serialization;

var registry = new EventTypeRegistry()
    .Register<CounterCreated>("counter.created")
    .Register<CounterIncremented>("counter.incremented");

IEventCodec codec = new SystemTextJsonEventCodec(registry);
```

The key is part of your permanent storage contract. Prefer a bounded-context and event-oriented convention such as `billing.invoice-issued`.

## 5. Save and reload

```csharp
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;

var eventStore = new InMemoryEventStoreProvider();
var repository = new EventSourcingRepository(eventStore, codec);
var cancellationToken = CancellationToken.None;

var id = new CounterId(Guid.NewGuid());
var counter = new Counter(id);
counter.Increment(2);

await repository.SaveAsync(counter, cancellationToken);

var reloaded = await repository.GetByIdAsync<Counter, CounterId>(
    id,
    cancellationToken);

Console.WriteLine(reloaded!.Value); // 2
Console.WriteLine(reloaded.CurrentVersion); // 1
Console.WriteLine(reloaded.LastCommittedVersion); // 1
```

The repository encodes the two uncommitted events, appends them atomically with expected version `-1`, and marks them committed only after the provider succeeds. Reloading reads and decodes the stream and replays both events.

## 6. Observe optimistic concurrency

If two aggregate instances load the same committed version and both save changes, only the first append can satisfy the expected version. The second save fails with `EventStoreConcurrencyException`. Reload the aggregate, re-evaluate the command against current state, and retry only when that behavior is valid for the use case.

## Next step

Read [Where to go next](next-steps.md), then use the [in-memory application tutorial](../tutorials/in-memory-application.md) to add commands and dependency injection.
