# Build an in-memory application slice

This tutorial extends the [quickstart aggregate](../getting-started/quickstart.md) with a command, handler, and Microsoft DI composition root.

## Project references

Add:

```text
DDD.BuildingBlocks.Core 2.0.0
DDD.BuildingBlocks.DevelopmentPackage 2.0.0
DDD.BuildingBlocks.DI.Extensions 2.0.0
```

## Define a command

Commands specify sourcing mode and aggregate identity. A create command expects no existing stream:

```csharp
using DDD.BuildingBlocks.Core.Commanding;

public sealed class CreateCounterCommand : Command
{
    public CreateCounterCommand(Guid counterId)
        : base(counterId.ToString("D"), targetVersion: -1)
    {
        CounterId = counterId;
        Mode = AggregateSourcingMode.Create;
    }

    public Guid CounterId { get; }
}
```

For updates, carry the expected version supplied by the caller or load the current state according to your application protocol. Do not silently convert every concurrency conflict into “last write wins.”

## Implement the handler

```csharp
using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Persistence.Repository;

public sealed class CreateCounterCommandHandler(IEventSourcingRepository repository)
    : CommandHandler<CreateCounterCommand>(repository)
{
    public override async Task HandleAsync(
        CreateCounterCommand command,
        CancellationToken cancellationToken)
    {
        var counter = new Counter(new CounterId(command.CounterId));
        await AggregateRepository.SaveAsync(counter, cancellationToken);
    }
}
```

Keep the handler thin. Domain validation belongs in the aggregate/value objects; provider and transaction mechanics belong behind repository contracts.

## Compose the application

```csharp
using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Event.Serialization;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.Core.Persistence.Storage;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using DDD.BuildingBlocks.DI.Extensions.Dispatching;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddSingleton<IEventCodec>(_ => new SystemTextJsonEventCodec(
    new EventTypeRegistry()
        .Register<CounterCreated>("counter.created")
        .Register<CounterIncremented>("counter.incremented")));

services.AddSingleton<IEventStoreProvider, InMemoryEventStoreProvider>();
services.AddSingleton<IEventSourcingRepository>(provider =>
    new EventSourcingRepository(
        provider.GetRequiredService<IEventStoreProvider>(),
        provider.GetRequiredService<IEventCodec>()));

services.AddDddBuildingBlocksDispatching(
    typeof(CreateCounterCommandHandler).Assembly);

await using var root = services.BuildServiceProvider(validateScopes: true);
var dispatcher = root.GetRequiredService<ICommandDispatcher>();

var id = Guid.NewGuid();
await dispatcher.DispatchAsync(
    new CreateCounterCommand(id),
    CancellationToken.None);
```

`AddDddBuildingBlocksDispatching` validates handler coverage at startup and creates a fresh DI scope for each dispatch.

## Query the aggregate for a decision

Aggregates are not read models, but an application service may load one to perform another command:

```csharp
var repository = root.GetRequiredService<IEventSourcingRepository>();
var counter = await repository.GetByIdAsync<Counter, CounterId>(
    new CounterId(id),
    CancellationToken.None);

counter!.Increment(3);
await repository.SaveAsync(counter, CancellationToken.None);
```

For user-facing queries, build a projection rather than exposing aggregate state directly.

## Test the slice

Use a fresh `InMemoryEventStoreProvider` per test unless the test intentionally covers multiple commands in one history. Assert domain state after repository reload, not only immediately after calling the aggregate method. This proves event codec and replay behavior.

## Production transition

Keep event registry and repository registration unchanged. Replace `IEventStoreProvider` with the PostgreSQL or SQL Server provider and run its migrator. Continue with the corresponding provider tutorial.
