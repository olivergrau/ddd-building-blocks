# Persona Simulation Playground

## DDD.BuildingBlocks framework analysis

**Status:** REVIEWED, modernization in progress  
**Version:** 1.1  
**Date:** 2026-08-16  
**Repository:** `olivergrau/ddd-building-blocks`  
**Reference commit:** `26faa4b7226f070a30ae7bb8e1a4cf79b0bba5ad`

## 1. Result in one sentence

`DDD.BuildingBlocks` is a useful and technically appropriate starting point, but should not be adopted unchanged into the playground. **Option B: targeted modernization** is recommended, whereby aggregate, value object, event sourcing and snapshot basic ideas are retained and persistence, event contracts, projections, cancellation and DI are specifically hardened.

No contradiction was found that would fundamentally block the planned playground.

## 2. Evaluation standard

The analysis checks the framework against the already binding decisions:

- tactical types from `DDD.BuildingBlocks` are used in the domain model;
- all genuine Playground domain aggregates use event sourcing, including `Session`, `Persona`, `Scenario`, and `PersonaRelationship`;
- relational state remains appropriate for projections, checkpoints, operational state, authentication, and other non-aggregate data;
- CQRS and reconstructable read models are mandatory;
- PostgreSQL is the preferred starting hypothesis for the production event-store provider;
- Persistence happens before publishing;
- the application remains a modular monolith in a .NET process;
- Domain apply must be deterministic and free of side effects;
- Projection recovery and complete rebuild must be possible;
- stale inference results must be averted via Optimistic Concurrency.

## 3. Repository and packages overview

| Package | Purpose | Review for Playground |
|---|---|---|
| `DDD.BuildingBlocks.Core` | Aggregates, Entities, Value Objects, Commands, Events, Event Sourcing Repository, Snapshots | Use and modernize base |
| `DDD.BuildingBlocks.DevelopmentPackage` | In-memory provider, file-based provider, in-memory publication | For testing and local development only |
| `DDD.BuildingBlocks.MSSQLPackage` | SQL Server event store, snapshots, event processing | Do not use it in production, but retain it as a reference |
| `DDD.BuildingBlocks.AzurePackage` | Azure Service Bus and Blob Storage | Not needed for the MVP |
| `DDD.BuildingBlocks.DI.Extensions` | DI Help and Service Locator | Significantly reduce or replace |
| `DDD.BuildingBlocks.Hosting.Background` | Hosted-service helpers | Use selectively only after modernization |

The examples `RocketLaunch` and `LunarOps` show aggregates, commands, domain services, snapshots, read model projectors and tests. They are valuable as examples of use, but are not normative architectural templates in all respects.

## 4. Positive findings

### 4.1 Tactical DDD types

Available are:

- `Entity<TKey>`;
- `EntityId<TKey>`;
- `ValueObject<T>`;
- `AggregateRoot<TKey>`;
- `DomainRelation`;
- domain-specific error classes.

This basically fits the desired explicit modeling with IDs, entities and value objects.

### 4.2 Event sourcing core mechanics

The framework supports:

- uncommitted events in the aggregate;
- immediate application to new events;
- Replay of historical events;
- Aggregate versions;
- expected version at append;
- Event sourcing repository;
- Provider abstraction;
- optional snapshots;
- In-memory provider for testing.

The basic semantics are compatible with the planned Playground aggregates.

### 4.3 Optimistic Concurrency

The framework has both a pre-check in the repository and an atomic version check in the MSSQL provider. The important part is the check within the storage transaction. This principle can be preserved and improved in the PostgreSQL provider.

This allows the planned protection against stale inference results to be mapped:

```text
Turn is based on Session version 41
Human pauses Session, creating version 42
Turn attempts append with ExpectedVersion 41
Append is rejected
Inference result is discarded
```

### 4.4 Snapshots

Snapshots are optional and are stored separately from the event stream. This corresponds to the decision that snapshots are just performance optimizations.

### 4.5 Examples and Tests

The repository contains:

- Unit tests for aggregates, entities, value objects, commands and event notification;
- Integration tests for in-memory and MSSQL persistence;
- Concurrency and stress testing for MSSQL;
- Snapshot testing;
- Projector tests in the Examples;
- API and application examples.

This is a good starting point for controlled modernization.

## 5. Capability Matrix

| Requirement | Stand | Rating | Measure |
|---|---|---|---|
| Tactical DDD Types | available | suitable | Gently modernize API |
| Event-sourced Aggregates | available | suitable with changes | Harden metadata, type identity, versioning |
| Aggregate-root naming | `AggregateRoot<TKey>` is the only root type | suitable | retain the established public name |
| Conventional aggregate abstraction | not available | intentionally unnecessary | keep the framework focused on event-sourced domain aggregates |
| Optimistic Concurrency | available | usable | make atomic provider testing mandatory |
| Uncommitted Events | available | suitable | ensure immutable exposure |
| Event Metadata | partially | inadequate | Add envelope |
| Event Schema Version | `ClassVersion` exists | partially | stable event names and upcaster concept complement |
| Event type resolution | CLR/name reflection | risky | Use stable logical type keys |
| Cancellation | largely missing | inadequate | Add `CancellationToken` throughout |
| Async | available | partially | Modernize contracts and error handling |
| PostgreSQL | missing | gap | implement new provider |
| In-memory tests | available | suitable with hardening | Align Thread Safety and Concurrency |
| Snapshots | available | usable | harden atomic semantics and compatibility |
| Persist-before-publish | in the MSSQL path basically given | partially | about durable event position and dispatcher guarantee |
| Projection Checkpoints | global offset exists | inadequate | redesign via projection, transactionally and idempotent |
| Projection Rebuild | not sufficiently formalized | gap | Add reset, replay and versioning |
| DI | Reflection plus Service Locator | risky | use explicit registration |
| Nullable | enabled, but warnings remain | partial | require a warning-free build at the gate |
| Linux/Containers | Core platform neutral | suitable | PostgreSQL testing in Linux containers |
| .NET version | `net9.0` | supported in the short term | upgrade to .NET 10 LTS |

## 6. Key technical risks and solutions

### F-01: `AggregateRoot<TKey>` represents the framework's event-sourced aggregate model

The existing `AggregateRoot<TKey>` implements `IEventSourcingBasedAggregate`. Every genuine domain aggregate supported by this focused framework is reconstructed and persisted through an event stream.

**Impact:** No ambiguity remains once conventional aggregate persistence is outside the framework scope. Introducing a second name would create migration cost without distinguishing two supported root models.

**Decision:** Keep the established `AggregateRoot<TKey>` name. Do not introduce `ConventionalAggregateRoot<TKey>`, `PlainAggregateRoot<TKey>`, or a marker-only `IAggregateRoot<TKey>` without a demonstrated framework use case.

```text
Entity<TKey>
  └── AggregateRoot<TKey>
```

All genuine Playground domain aggregates use this event-sourced model. Relational tables remain valid for projections, checkpoints, operational state, and other data that is not an aggregate source of truth.

### F-02: Persisted event types are attached to CLR names

Events are identified using simple class names and sometimes `AssemblyQualifiedName`. Simple names can clash. Assembly names and versions are not stable technical contracts.

**Suggested solution:** Persisted `EventType` becomes an explicit stable string, for example `session.participant-spoke`. An explicit registry maps it to CLR types. `SchemaVersion` is stored separately. Upcasters read older schemas.

### F-03: Event metadata is incomplete

Available are aggregate ID, target version, commit time, correlation ID and class version. In particular, the following are missing:

- `EventId`;
- `CausationId`;
- `CommandId`;
- global event position;
- clean actor reference;
- optional `TurnId` and technical inference reference.

**Suggested solution:** Domain events and the persisted `EventEnvelope` are separated. The event contains only domain payload. The envelope carries technical metadata.

### F-04: Projection offset is not robust enough

The MSSQL worker uses a numeric row offset across a query sorted by time and stream version. This is unsuitable for resilient recovery:

- Timestamp and stream version do not form a guaranteed unique global order;
- OFFSET is position-related and can be problematic when the amount of data changes;
- Progress is managed per worker, not cleanly per projection contract;
- Idempotence is not secured by an event identifier;
- Rebuild and parallel operation are not sufficiently formalized.

**Suggested solution:** The PostgreSQL event store receives a monotonic global `position BIGINT`. Each projection maintains a checkpoint over `(projection_name, projection_version, last_position)`. Event application and checkpoint updates occur in the same read-store transaction. Duplicate delivery is reliably detected via `EventId` or position.

### F-05: Cancellation is missing from the core contracts

Repository, storage, command and event handler methods mostly do not accept a `CancellationToken`.

**Proposed solution:** Cancellation is added at all I/O and orchestration boundaries. Domain methods themselves do not require a `CancellationToken` as long as they remain purely synchronous and deterministic.

### F-06: Reflection and Service Locator

Command and event handlers are sometimes found or created via reflection, `Activator` and an optional service locator.

**Suggested solution:** Use explicit DI registration and typed dispatchers. Reflection can only be used at startup for validated registration, not as a hidden runtime dependency.

### F-07: In-memory provider does not fully correspond to production behavior

The internal collections are not fully thread-safe. Concurrency and Publication are different from the MSSQL path.

**Proposed solution:** The in-memory provider is made deterministic and thread-safe and must enforce the same expected-version rules as PostgreSQL. It remains a test provider and is not production persistence.

### F-08: Event publication and snapshot errors

With the in-memory path, events can enter a queue before the repository flow is fully completed. Snapshots are saved after event commit. A snapshot error must not semantically undo a successful event commit.

**Suggested solution:** The event commit is the only domain commit. Snapshot errors are handled separately. Production projections read only committed events from the event store. In-memory publishing is aligned with the same semantics.

### F-09: Version model and time types

The framework uses `int` and a zero-based internal event version. This is technically functional, but ambiguous in contracts. Times are `DateTime`.

**Suggested solution:**

- Clearly document stream version internally: `NoStream = -1`, first event creates version `0`;
- provide `BIGINT` and C# `long` for new persistence columns;
- Use `DateTimeOffset` or UTC `Instant` semantics;
- Do not confuse UI sequences with aggregate versions.

### Q-10: .NET target version

The repository targets .NET 9. At the time of analysis, .NET 10 is the current LTS version and is supported until November 2028. .NET 9 is STS and only supported until November 2026.

**Suggested solution:** Align framework and playground to .NET 10 LTS. Source: Microsoft's [.NET Releases and Support](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support).

## 7. Dependency direction

Direct domain dependency on `DDD.BuildingBlocks.Core` is explicitly allowed according to Architecture Decisions. It remains justifiable if Core only contains tactical domain and ES basic types.

Transitive dependencies of the domain project on:

- ASP.NET Core;
- EF Core or Npgsql;
- HTTP;
- vLLM/OpenAI DTOs;
- concrete repository providers;
- Background hosting.

Today's core dependency on logging and Newtonsoft.Json should be reduced. Serialization belongs in providers or event codec components, not in the domain base types.

## 8. Build and Test Proof

Static code analysis was performed on the mentioned commit. The available working environment did not have a `dotnet` SDK installed, so build and testing could not be run here.

This is not a modeling blocker, but a binding gate before the first framework change:

```text
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Additionally, PostgreSQL integration tests via test containers are required.

## 9. Usage decision

### Decision: targeted modernization

Should be used:

- Entity, ID and Value Object basic ideas;
- Event-sourced aggregate mechanics;
- uncommitted events and replay;
- provider limit;
- Optimistic concurrency as a principle;
- optional snapshots;
- Given-When-Then testing style.

Should be specifically replaced or expanded:

- Event envelope and metadata;
- stable event type registry;
- PostgreSQL provider;
- Projection Dispatch, Checkpoints and Rebuild;
- Cancellation;
- DI integration;
- explicit documentation of the event-sourced `AggregateRoot<TKey>` contract;
- serialization;
- Time and version semantics.

The following should not be adopted:

- Azure packages for the MVP;
- MSSQL as a production provider;
- Service locator based runtime resolution;
- position-based projection offset;
- Assembly qualified names as persistent event contracts.

## 10. Consequence for work package 3

The domain contracts are not adapted to problematic details of the current framework. Instead, the following guidelines apply:

- all genuine domain aggregates are modeled as event-sourced;
- relational persistence is reserved for projections and non-aggregate technical or operational state;
- Events have technically small payloads;
- Metadata is in the envelope;
- Commands have `CommandId` and an expected version when changes occur;
- Apply remains synchronous, deterministic and side effect free;
- C# sketches in Phase 3B show target semantics and are not an exact copy of today's framework API.
