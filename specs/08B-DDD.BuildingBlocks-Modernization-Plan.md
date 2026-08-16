# Persona Simulation Playground

## DDD.BuildingBlocks modernization plan

**Status:** ACTIVE — M3/F2 implemented; awaiting Gate G2 review
**Version:** 1.3
**Date:** 2026-08-16  
**Base:** Framework analysis on commit `26faa4b7226f070a30ae7bb8e1a4cf79b0bba5ad`

## 1. Goal

The framework will be brought to a robust .NET 10 baseline before production use. Modernization remains a separate, incremental preliminary project. It must neither preempt the Playground nor introduce a second event-sourcing infrastructure alongside it.

## 2. Binding boundaries

### In scope

- .NET 10 LTS and current compatible packages;
- Warning-free and nullable hardening;
- Cancellation in I/O contracts;
- explicit event-sourced aggregate semantics;
- Event envelope and stable event type registry;
- Optimistic Concurrency Contract;
- production event-store adapter and snapshot provider after an explicit technology decision;
- resilient projection checkpoints and rebuild;
- In-memory test provider with production-level semantics;
- explicit DI registration;
- Testing, migrations and documentation.

### Out of scope

- general message broker support;
- preemptive support for multiple production event-store technologies at the same time;
- Multi-tenant support;
- distributed sagas;
- Actor Runtime;
- Azure modernization without the need for Playground;
- generic plugin system;
- automatic event migration of all conceivable old versions.

## 3. Target architecture of the framework core

```text
DDD.BuildingBlocks.Core
  Domain primitives
  Event-sourced aggregate primitives
  Event envelope contracts
  Command/result contracts
  Repository/provider contracts

DDD.BuildingBlocks.<ChosenProvider>
  Event store adapter
  Snapshot store adapter
  Projection feed/checkpoints
  provider-specific migrations or configuration

DDD.BuildingBlocks.Testing
  In-memory providers
  Given-When-Then harness

DDD.BuildingBlocks.DependencyInjection
  Explicit registrations
```

## 4. Stages

### M1: Reproduce baseline

**Goal:** Build and test the current version unchanged.

**Acceptance:**

- SDK version pinned;
- Restore, build and tests reproducible;
- Test categories documented;
- known failing tests are explained, not ignored.

**Stop point:** No technical changes.

### M2: .NET 10 and compiler hardening

**Goal:** Technical update without changing semantics.

**Acceptance:**

- all relevant projects on `net10.0`;
- central package versions consistent;
- Nullable enabled;
- Warnings as Errors for your own code;
- Linux build successful.

### M3: Aggregate Root and Core Contracts

**Goal:** Make the framework's event-sourcing focus explicit in its type model.

**Acceptance:**

- existing `AggregateRoot<TKey>` name retained as the framework's single aggregate-root model;
- no conventional or plain aggregate-root abstraction introduced;
- no marker-only aggregate-root interface introduced without a concrete consumer;
- uncommitted events only exposed in a readable manner;
- replay and application of new events clearly separated;
- tests for version transitions and missing apply handlers;
- documentation explicitly describes the event-sourcing semantics of the root type.
- the existing `int` stream-version contract is preserved until its coordinated `long` migration with event contracts, snapshots, providers, and storage schemas after Gate G2.

### M4: Event Envelope and Event Codec

**Goal:** Decouple persisted contracts from CLR type names.

Minimum envelope:

```text
EventId
StreamId
AggregateType
StreamVersion
GlobalPosition
EventType
SchemaVersion
OccurredAt
CommittedAt
CorrelationId
CausationId
CommandId optional
TurnId optional
Actor optional
Payload
```

**Acceptance:**

- stable logical event names;
- explicit registry;
- unknown event types lead to classified errors;
- at least one upcaster test;
- no assembly qualified names in new records.

### M5: Async and cancellation

**Goal:** Controllable I/O operations.

**Acceptance:**

- `CancellationToken` in repository, provider, snapshot, dispatcher and handler;
- Cancellation is not classified as a domain error;
- no cancellation in pure domain apply methods;
- Tests for abort before and during I/O.

### M6: Persistence Decision Gate and Production Event Store

**Goal:** Select an event-store technology based on mandatory capabilities and operational effort, then implement exactly one production adapter.

Options under evaluation:

```text
DDD.BuildingBlocks PostgreSQL Provider
Adaptation of the existing relational provider logic
external event store with a DDD.BuildingBlocks adapter
```

PostgreSQL remains the preferred starting hypothesis as long as an external solution does not offer any demonstrable additional benefit.

For a relational implementation, at least the following storage classes are required:

```text
event_streams
events
snapshots
projection_checkpoints
```

**Acceptance:**

- unique stream via aggregate type and aggregate ID;
- atomic expected version check;
- append multiple events in one transaction;
- monotone global position;
- Event ID unique;
- Stream reading from version and feed reading from position;
- reproducible migrations or equivalent store configuration;
- Testcontainers tests for Create, Append, Conflict, Replay and concurrency.

### M7: Projection Pipeline

**Target:** Persist-before-publish, recovery and rebuild.

**Acceptance:**

- committed event feed from the selected production store;
- Checkpoint per projection and projection version;
- idempotent redelivery;
- Projection update and checkpoint in one transaction;
- Error only stops the affected projection;
- Rebuild from position zero;
- no global position-based line offset.

### M8: Snapshots

**Goal:** Optional, disposable rehydration optimization.

**Acceptance:**

- Snapshot is associated with a stream version;
- incompatible snapshot can be discarded;
- Rehydration without snapshot remains completely possible;
- Snapshot failure does not change successful event commit;
- Equivalence test event replay against snapshot plus residual events.

### M9: DI and dispatch

**Goal:** Explicit, verifiable handler resolution.

**Acceptance:**

- no service locator in the normal runtime path;
- Command and projection handlers explicitly registered;
- duplicate handler registration is detected at startup;
- scoped dependencies work;
- Reflection discovery is optional and validated.

### M10: Release gate

**Goal:** Release the first playground slice.

**Acceptance:**

- all unit and integration tests green;
- API documentation updated;
- Migration and recovery test successful;
- small Playground-oriented example with at least two event-sourced aggregates with different lifecycles, such as `Session` and `Persona`;
- Framework version pinned;
- no open critical finding category.

## 5. Prioritization

| Priority | Points |
|---|---|
| Blocking before Playground implementation | M1 to M5 and M9 for the required core size |
| Blocking before production domain-aggregate persistence | M6 |
| Blocking before production read models | M7 |
| Before activating Snapshots | M8 |
| Before broad application integration | M9 |
| Before the first production slice | M10 |

## 6. Deliberately postponed questions

- Whether a shared aggregate interface is ever needed; none is introduced without a concrete consumer.
- Whether the event codec uses `System.Text.Json` or a replaceable codec interface in the long term. An explicitly versioned codec is sufficient for the playground.
- Whether the event store and read models use the same PostgreSQL instance or whether the event store is later operated externally. For the MVP, a shared PostgreSQL instance remains the simpler starting hypothesis.
- From which stream length snapshots are activated. This is measured, not guessed.

## 7. Decision rule in case of conflicts

When framework compatibility and playground invariants collide:

1. Domain and persistence invariants of the Playground are preserved.
2. The framework is adapted.
3. A parallel second ES infrastructure is not permitted.
4. Breaking framework changes are only made when a compatible addition would obscure the semantics.
