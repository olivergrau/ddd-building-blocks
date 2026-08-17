# DDD.BuildingBlocks documentation

DDD.BuildingBlocks is an opinionated .NET framework for tactical Domain-Driven Design with event-sourced aggregates. It provides domain primitives, versioned event contracts, event-store abstractions, production PostgreSQL and SQL Server adapters, snapshots, projection recovery, and dependency-injection-based dispatch.

The framework deliberately supports one aggregate persistence model: domain aggregates are event sourced. Relational tables remain a good fit for projections, read models, checkpoints, and operational state, but not as an alternative state-storage mechanism for aggregate roots.

## Choose a starting point

- [Getting started](getting-started/index.md) explains prerequisites, package installation, and the smallest complete event-sourced aggregate.
- [Concepts](concepts/index.md) explains how aggregates, events, streams, snapshots, projections, and dispatch fit together.
- [Tutorials](tutorials/index.md) build complete slices using the in-memory, PostgreSQL, and SQL Server providers.
- [Guides](guides/index.md) cover event evolution, testing, recovery, database migration, operations, and the 1.x-to-2.0 upgrade.
- [Reference](reference/index.md) summarizes packages, composition-root responsibilities, examples, and terminology.

## Framework shape

```text
Domain behavior
  AggregateRoot<TKey>
  Entity<TKey> and ValueObject
  DomainEvent and stable event contracts
             |
             v
EventSourcingRepository
  IEventCodec
  IEventStoreProvider
  optional ISnapshotCodec + ISnapshotStoreProvider
             |
             +-----------------------------+
             |                             |
             v                             v
PostgreSQL / SQL Server / In-Memory   committed event feed
                                           |
                                           v
                                  ProjectionRunner
                                  checkpoint + read-model transaction
```

## Core guarantees

- New state changes are expressed as domain events through `RaiseEvent`.
- Rehydration replays committed events through aggregate-internal handlers.
- Event storage uses stable keys and versioned JSON envelopes rather than CLR assembly names.
- Appends use optimistic expected-version concurrency.
- A committed feed gives projections a monotonic recovery cursor.
- Projection updates and checkpoint advancement share one transaction.
- Snapshots are disposable optimizations; full event replay remains authoritative.
- Asynchronous framework contracts require cancellation tokens.

## Version and distribution

These pages describe DDD.BuildingBlocks `2.0.0`, targeting .NET 10. Packages are downloadable from the project's GitHub Release and can be installed through a local NuGet source. They are not published to NuGet.org.

Existing 1.x consumers must read the [upgrade guide](guides/upgrading-from-1.x.md) before changing package references or databases.
