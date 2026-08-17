# Commands and dependency-injection dispatch

## Commands

A command expresses requested intent. It is not a historical fact. Commands may be rejected because current aggregate state no longer permits the operation.

The framework's command model carries aggregate identity, expected version, correlation data, and an `AggregateSourcingMode` such as create or load.

## Handlers

Command handlers implement `ICommandHandler<TCommand>` or derive from `CommandHandler<TCommand>`. They typically:

1. validate input or create domain value objects;
2. source the aggregate;
3. call one business method;
4. save the aggregate through `IEventSourcingRepository`;
5. propagate the cancellation token.

Handlers coordinate; aggregates decide.

## Registration

Register handler assemblies at startup:

```csharp
services.AddDddBuildingBlocksDispatching(
    typeof(RegisterMissionCommandHandler).Assembly,
    typeof(MissionProjector).Assembly);
```

Startup scanning registers closed command handlers and event subscribers. Registration fails if a command has no handler, a command has multiple handlers, or the same subscriber implementation is duplicated for an event.

## Scope behavior

`ScopedCommandDispatcher` and `ScopedDomainEventNotifier` are singleton entry points that create an independent service scope for every dispatch. Handlers and subscribers are registered as scoped services, so scoped database contexts and other scoped dependencies are not captured by a singleton.

## Cancellation

Dispatch methods require a cancellation token. Pass request-abort, worker-stop, or operation-timeout tokens from the outer boundary. Cancellation is not an application error and remains unwrapped.

## Domain events versus integration events

Domain events are persisted facts inside a bounded context. Integration events are external contracts published after the domain commit. Do not expose internal event payloads as public integration contracts by default; their versioning and audience differ.
