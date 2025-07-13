# Quickstart

This quick introduction shows how to get started with **DDD.BuildingBlocks.Core**. It demonstrates referencing the framework, defining a simple aggregate and handling commands. A minimal read model is also included so you can see the full flow from domain events to queryable state.

## 1. Install the packages

Use the .NET CLI to add the core building blocks and the in-memory helpers (useful for development):

```bash
dotnet add package DDD.BuildingBlocks.Core
# optional development helpers
dotnet add package DDD.BuildingBlocks.DevelopmentPackage
```

## 2. Define your aggregate

Create domain types for the `Order` aggregate. Below is an illustrative snippet with an `OrderId` value object, an `OrderPlaced` domain event and the `OrderAggregate` itself.

```csharp
// Value object for the aggregate key
public sealed class OrderId : EntityId<OrderId>
{
    public Guid Value { get; }
    public OrderId(Guid value) => Value = value;
    // string constructor for aggregate sourcing
    public OrderId(string value) => Value = Guid.Parse(value);
    protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
    {
        yield return Value;
    }
}

// Domain event emitted when a new order is created
[DomainEventType]
public sealed class OrderPlaced : DomainEvent
{
    private const int CurrentClassVersion = 1;
    public OrderId OrderId { get; }
    public DateTime PlacedAt { get; }

    public OrderPlaced(OrderId orderId, DateTime placedAt, int targetVersion = -1)
        : base(orderId.Value.ToString(), targetVersion, CurrentClassVersion)
    {
        OrderId  = orderId;
        PlacedAt = placedAt;
    }
}

// Aggregate root
public class OrderAggregate : AggregateRoot<OrderId>
{
    public DateTime? PlacedAt { get; private set; }

    public OrderAggregate(OrderId id) : base(id)
    {
        ApplyEvent(new OrderPlaced(id, DateTime.UtcNow));
    }

    // Needed for rehydration
    public OrderAggregate() : base(default!) { }

    [InternalEventHandler]
    private void On(OrderPlaced e)
    {
        PlacedAt = e.PlacedAt;
    }

    protected override OrderId GetIdFromStringRepresentation(string value) => new(value);
}
```

## 3. Handle a command

Commands encapsulate requests to mutate an aggregate. Implement a command and handler using the provided abstractions:

```csharp
// Command to place a new order
public sealed class PlaceOrderCommand : Command
{
    public PlaceOrderCommand(Guid orderId)
        : base(orderId.ToString(), -1)
    {
        Mode = AggregateSourcingMode.Create;
    }
}

// Command handler
public sealed class PlaceOrderCommandHandler(IEventSourcingRepository repository)
    : CommandHandler<PlaceOrderCommand>(repository)
{
    public override async Task HandleCommandAsync(PlaceOrderCommand command)
    {
        var aggregate = await AggregateSourcing.Source<OrderAggregate, OrderId>(command);
        await AggregateRepository.SaveAsync(aggregate!);
    }
}
```

Register the handler with an `ICommandProcessor` and execute the command:

```csharp
var processor = new DefaultCommandProcessor();
processor.RegisterHandlerFactory(() => new PlaceOrderCommandHandler(repository));
await processor.ExecuteAsync(new PlaceOrderCommand(Guid.NewGuid()));
```

## 4. Build a read model

Project domain events into a queryable representation. Implement a projector that subscribes to events and writes to a custom service.

```csharp
public interface IOrderReadService
{
    Task<OrderReadModel?> GetByIdAsync(Guid id);
    Task CreateOrUpdateAsync(OrderReadModel model);
}

public sealed class OrderProjector(IOrderReadService service)
    : ISubscribe<OrderPlaced>
{
    public async Task WhenAsync(OrderPlaced e)
    {
        var model = new OrderReadModel
        {
            OrderId  = e.OrderId.Value,
            PlacedAt = e.PlacedAt
        };
        await service.CreateOrUpdateAsync(model);
    }
}
```

This read model receives `OrderPlaced` events and stores a simplified view which can be queried separately from the aggregate.

---

## 5. Add multiple business methods

Aggregates often require more than just one command. You can freely define **multiple business methods** that:

* Enforce invariants and rules,
* Emit domain events via `ApplyEvent(...)`,
* Use injected services for domain-level validation (e.g. availability checks).

Here’s a more advanced example using a `Mission` aggregate:

```csharp
public class Mission : AggregateRoot<MissionId>
{
    public MissionName Name { get; private set; }
    public MissionStatus Status { get; private set; }
    public RocketId? AssignedRocket { get; private set; }

    public Mission(MissionId id, MissionName name)
        : base(id)
    {
        ApplyEvent(new MissionCreated(id, name));
    }

    public async Task AssignRocketAsync(Rocket rocket, IResourceAvailabilityService validator)
    {
        if (Status != MissionStatus.Planned)
            throw new AggregateValidationException(Id, nameof(Status), Status, "Can only assign rocket in Planned state");

        if (!await validator.IsRocketAvailableAsync(rocket.Id))
            throw new RuleValidationException(Id, "Rocket not available", $"RocketId: {rocket.Id}");

        ApplyEvent(new RocketAssigned(Id, rocket.Id, CurrentVersion));
    }

    public void Schedule()
    {
        if (AssignedRocket is null)
            throw new AggregateException(Id, "Rocket not assigned");
        if (Status != MissionStatus.Planned)
            throw new AggregateException(Id, "Already scheduled");

        ApplyEvent(new MissionScheduled(Id, CurrentVersion));
    }

    [InternalEventHandler]
    private void On(MissionCreated e)
    {
        Name = e.Name;
        Status = MissionStatus.Planned;
    }

    [InternalEventHandler]
    private void On(RocketAssigned e)
    {
        AssignedRocket = e.RocketId;
    }

    [InternalEventHandler]
    private void On(MissionScheduled e)
    {
        Status = MissionStatus.Scheduled;
    }

    protected override MissionId GetIdFromStringRepresentation(string value) => new(Guid.Parse(value));
}
```

> ✅ **Best practice**: Make each method express a business decision. Don’t use setters. Always enforce domain rules before emitting events.

---

## 6. Work with domain relations (referencing other aggregates)

Sometimes you want to **refer to other aggregates** (e.g. `Rocket`, `CrewMember`, `LaunchPad`) **without loading them into memory**. For this, the framework provides `DomainRelation`.

```csharp
public List<DomainRelation> Crew { get; } = new();

// Example: assigning crew members to a mission
public async Task AssignCrewAsync(IEnumerable<CrewMemberId> crew, IResourceAvailabilityService validator)
{
    if (!await validator.AreCrewMembersAvailableAsync(crew))
        throw new RuleValidationException(Id, "Crew not available");

    ApplyEvent(new CrewAssigned(Id, crew, CurrentVersion));
}

[InternalEventHandler]
private void On(CrewAssigned e)
{
    Crew.AddRange(e.Crew.Select(id => new DomainRelation(id.Value.ToString())));
}
```

> 🧠 `DomainRelation` is a simple ValueObject wrapping another aggregate’s ID. This keeps aggregates clean and decoupled — especially useful in distributed or event-sourced systems.

You can rehydrate `DomainRelation` later when projecting or querying, but **you don’t model foreign aggregate state inside your own**.

---

This completes the quickstart for advanced scenarios. You now know how to:

* Define multiple business actions in one aggregate,
* Keep aggregates clean and rule-driven,
* Use domain relations instead of cross-aggregate loading.

For deeper architectural guidance, refer to the [Tactical DDD Concepts Guide](./tactical_ddd_guide.md).

---

With these pieces in place you can quickly model domains with commands, aggregates and read models using the building blocks provided by this framework.
