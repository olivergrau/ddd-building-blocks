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

With these pieces in place you can quickly model domains with commands, aggregates and read models using the building blocks provided by this framework.
