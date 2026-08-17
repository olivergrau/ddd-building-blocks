# Concepts

DDD.BuildingBlocks is easier to use when its boundaries are explicit. These articles explain the framework's model before provider-specific details.

- [Domain model](domain-model.md)
- [Event-sourcing lifecycle](event-sourcing.md)
- [Event contracts and evolution](event-contracts.md)
- [Repository and persistence providers](persistence.md)
- [Snapshots](snapshots.md)
- [Projections and recovery](projections.md)
- [Commands and dependency-injection dispatch](commands-and-dispatch.md)
- [Errors and cancellation](errors-and-cancellation.md)

The central rule is simple: aggregate state is reconstructed from its event stream. Snapshots may accelerate reconstruction, and projections may materialize query models, but neither replaces the event stream as the aggregate's source of truth.
