# Domain model

## Aggregate roots

`AggregateRoot<TKey>` represents a consistency boundary and the only entry point to its object graph. DDD.BuildingBlocks has no conventional state-persistence aggregate root. If an object is modeled as a domain aggregate in this framework, its durable changes are events.

An aggregate should:

- expose intention-revealing business methods;
- protect invariants before raising an event;
- avoid public setters for durable state;
- change durable state inside internal event handlers;
- reference other aggregates by identity rather than embedding their full mutable object graphs;
- remain independent of database, HTTP, hosting, and provider packages.

## Identifiers

Aggregate identifiers derive from `EntityId<TSelf>`. The identifier's `ToString()` result becomes the stream ID. `GetIdFromStringRepresentation` must reconstruct the same identifier during first-event replay.

Treat this string format as durable. Changing it after events exist changes stream identity.

## Entities

`Entity<TKey>` supports identity-based equality. Equality accounts for the runtime entity type, so unrelated entity types with equal key values do not become equal accidentally.

Entities inside an aggregate are controlled by the aggregate root. They are not independently loaded or saved through the event-sourcing repository.

## Value objects

`ValueObject` supports structural equality through `GetAttributesToIncludeInEqualityCheck`. Good value objects are immutable, validate themselves at construction, and represent a domain concept rather than a technical container.

Examples in RocketLaunch include `MissionName`, `LaunchWindow`, and `TargetOrbit`. LunarOps includes `VehicleType` and `LunarPayload`.

## Relations to other aggregates

`DomainRelation` stores a serialized reference to another domain object. Use it when an aggregate needs to remember another aggregate's identity without taking ownership of that aggregate's state.

Cross-aggregate rules often require a domain service or an application-provided availability port. RocketLaunch's mission behavior receives `IResourceAvailabilityService`; LunarOps uses services for docking, crew transfer, payload handling, and undocking.

Do not load several aggregates and pretend they share one atomic event-stream transaction. Design eventual coordination explicitly.

## Event handlers inside aggregates

Each concrete event applied to an aggregate requires one internal handler discovered through `InternalEventHandler` metadata. Handlers should be deterministic and free of external I/O because they run during both new-event application and historical replay.

```csharp
[InternalEventHandler]
private void On(MissionScheduled @event)
{
    Status = MissionStatus.Scheduled;
}
```

The handler receives a fact; it does not decide whether the fact should happen. That decision belongs in the business method that calls `RaiseEvent`.

## Deactivation

An aggregate can expose domain behavior that marks itself deactivated. Once the root is deactivated, later events cannot be applied. Model this as an explicit domain lifecycle decision and ensure the closing event itself is applied before the deactivated flag prevents further events.
