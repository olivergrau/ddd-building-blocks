# DDD.BuildingBlocks

## Detailed implementation plan for modernization and playground release

**Status:** ACTIVE — F1 complete; F2 architecture decided  
**Version:** 1.1  
**Date:** 2026-08-16  
**Initial status:** Commit `26faa4b7226f070a30ae7bb8e1a4cf79b0bba5ad`

## 1. Goal

DDD.BuildingBlocks is brought to a resilient .NET 10 level in a standalone repository, provided as versioned NuGet packages and only then used by the Persona Simulation Playground.

The modernization retains the framework's viable foundations. It is not a rewrite under the same name. However, problematic technical coupling will not be carried into the Playground merely for compatibility.

## 2. Binding result structure

Final package names will be confirmed after inventory. Technically, the following separation is aimed for:

```text
DDD.BuildingBlocks.Core
  tactical domain primitives
  event-sourced aggregate roots
  Domain events and domain-neutral contracts

DDD.BuildingBlocks.EventSourcing
  Envelope, registry, codec
  Repository and Store Contracts
  Snapshots

DDD.BuildingBlocks.Projections
  committed event feed
  Projection Contracts
  Checkpoints, retry and rebuild

DDD.BuildingBlocks.DependencyInjection
  explicit registrations and startup validation

DDD.BuildingBlocks.Testing
  Given-When-Then Harness
  In-memory provider
  Provider Contract Suites

DDD.BuildingBlocks.<ProductionProvider>
  exactly one production event-store adapter
  snapshot and feed integration
```

This structure is a goal, not a preliminary decision for an immediate package split. If existing packages can be modernized cleanly, compatible evolution is cheaper than artificial reorganization.

## 3. Work rules

1. Each stage has its own branch or a clearly defined commit sequence.
2. The unchanged baseline is recorded before semantic changes.
3. Public API changes are checked with Compatibility Report and Migration Notice.
4. No Playground code is copied into the framework.
5. Playground-related examples may be technically abstract but may not contain any product logic.
6. Tests will be supplemented with the respective changes.
7. Packaging is checked early, not at the end.
8. A green build does not replace architectural approval.
9. Unplanned refactorings are noted as a subsequent step.
10. After each gate, Codex stops for review.

## 4. Phase F0: Baseline and reproducibility

### F0.1 Save reference level

- Confirm reference commit;
- Set branch protection and work branch;
- Inventory tags and existing package versions;
- list public packages and dependencies;
- Categorize examples and test projects;
- Save existing CI configuration.

### F0.2 Set build environment

- Pin `global.json` to desired .NET 10 SDK;
- Initially build the dev container without changing the code;
- Document restore, build, test and pack as reproducible commands;
- Set Linux as a mandatory development and CI platform;
- Check architecture dependencies for `linux-x64` and, if necessary, `linux-arm64`.

### Run F0.3 Baseline

```text
dotnet --info
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet pack DDD.BuildingBlocks.Packages.slnf --no-build --configuration Debug --output artifacts/packages
```

In addition, the number of tests, duration, warnings, excluded tests and external requirements are recorded.

### F0.4 result

A baseline report classifies each finding as:

```text
existing defect
modernization blocker
accepted technical debt
outdated example
missing test evidence
```

**Gate G0:** No change before shared review of the baseline report.

## 5. Phase F1: Build system and .NET 10

### F1.1 Central build configuration

- `Directory.Build.props` for Language, Nullable and Analyzers;
- `Directory.Packages.props` for Central Package Management;
- shared version and repository metadata;
- deterministic builds;
- SourceLink and repository information for packages;
- Symbol packages and XML documentation for public APIs.

### F1.2 Package update

Each update is classified:

- safe and mechanical;
- API customizable;
- semantically risky;
- no longer needed;
- only relevant for outdated providers.

Large package updates are not bundled into an undifferentiated commit.

### F1.3 compiler hardening

- Enable nullable for your own code;
- Clean up warnings gradually;
- `TreatWarningsAsErrors` for CI and custom code;
- handle generated code and unavoidable third-party warnings in a targeted manner;
- no broad `NoWarn` list as a shortcut.

### F1.4 Acceptance

- Build under Linux green;
- existing semantics confirmed by regression tests;
- Packet artifacts are generated;
- no unexplained warnings.

**Gate G1:** Accept technical migration separately from technical contract changes.

## 6. Phase F2: Tactical Domain Primitives

### F2.1 Inventory public API

For `Entity`, `EntityId`, `ValueObject`, Aggregate Roots, Domain Errors and event base types are documented:

- public and protected members;
- Equality semantics;
- Mutability;
- serialization assumptions;
- Reflection assumptions;
- known consumers in examples and tests.

### F2.2 Explicit Event-Sourced Aggregate Root

- Rename `AggregateRoot<TKey>` to `EventSourcedAggregateRoot<TKey>` immediately;
- treat the rename as an intentional breaking change and document consumer migration;
- do not add `ConventionalAggregateRoot<TKey>` or `PlainAggregateRoot<TKey>`;
- do not add a marker-only `IAggregateRoot<TKey>` without a concrete consumer;
- keep relational projections and technical or operational state outside the aggregate-root model;
- Clearly separate replay and creation of new events;
- unchangeably expose uncommitted events;
- Fully validate apply handler;
- Clearly define `NoStream` and first version;
- Prepare version range to `long`;
- Clear-Uncommitted only after a successful commit;
- no infrastructure or serializer dependency.

### F2.3 Tests

- Equality and typed IDs;
- migration of known consumers to `EventSourcedAggregateRoot<TKey>`;
- new event is applied exactly once;
- Replay does not generate uncommitted events;
- missing apply handler clearly fails;
- Order and version by replay;
- Commit errors preserve uncommitted events for controlled handling.

**Gate G2:** The tactical API is approved before every provider conversion.

## 7. Phase F3: Event contracts and evolution

### F3.1 Event Envelope

The envelope contains at least:

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
Actor optional
TurnId optional
Payload
```

The global position is assigned only during the production commit. Domain events do not have a store position.

### F3.2 Event Registry

- explicit stable type key;
- exactly one CLR assignment per current type key;
- duplicate keys lead to an error when starting;
- unknown keys result in a classified deserialization error;
- no new persisted assembly qualified names.

### F3.3 Codec and Upcasting

- `System.Text.Json` is the preferred default implementation;
- Providers store payload plus type and schema metadata;
- Upcasters work from one known schema version to the next;
- Original events will not be rewritten in place;
- historical JSON fixtures ensure compatibility.

### F3.4 Compatibility

Existing persisted MSSQL data should only be migrated if there is a real asset that needs to be preserved. Without such an inventory, a documented breaking change is sufficient. No speculative universal migration.

**Gate G3:** Explicitly remove persistence format and evolution strategy.

## 8. Phase F4: Async, Cancellation, Error and DI

### F4.1 I/O Contracts

`CancellationToken` is the last parameter at all I/O boundaries, with a default only where API compatibility requires it. Pure domain methods remain synchronous.

### F4.2 Error classification

At least separately:

```text
Validation
Domain Rejection
Stream Not Found
Stream Already Exists
Concurrency Conflict
Unknown Event Type
Unsupported Schema Version
Serialization Failure
Transient Provider Failure
Permanent Provider Failure
Cancellation
```

### F4.3 DI and Dispatch

- normal runtime without service locator;
- explicit registration;
- Startup validation for missing and duplicate handlers;
- Scoped dependencies correctly;
- Reflection at most as validated registration support at startup;
- Dispatcher without hidden global container reference.

### F4.4 Tests

- Cancellation before I/O;
- Cancellation during provider operation;
- Cancellation is not packaged as a domain error;
- double handler registration;
- missing handler;
- Scoped Lifetime;
- parallel dispatches without global state.

**Gate G4:** Remove application-compatible core contracts and error semantics.

## 9. Phase F5: In-memory provider and contract suite

### F5.1 Production-level semantics

- thread safe;
- atomic expected version check;
- unique EventIds;
- stable stream order;
- committed feed in global order;
- Cancellation;
- no publication before commit;
- no silent best effort deviations.

### F5.2 Provider Contract Suite

An abstract test set tests each event store provider:

- new stream;
- Append one or more events;
- Loading from version;
- incorrect expected version;
- parallel writers;
- double EventId;
- Cancellation;
- global position;
- Feed from position;
- immutable stored payload;
- Error and retry classification.

Separate contracts are created if necessary for snapshots and projection checkpoints. Conventional aggregate persistence is outside the framework scope.

**Gate G5:** The contract suite is accepted as normative provider semantics.

## 10. Phase F6: Production Provider Decision

### F6.1 Spike A: PostgreSQL adapter

The spike proves:

- atomic append with Expected Version;
- monotone global position;
- Stream Read;
- committed feed;
- Transaction and concurrency behavior;
- Migration, backup and restore;
- Operation via test containers.

### F6.2 Spike B: external event store, only if serious candidate

An external store is only examined separately if it promises a specific advantage. Additional operational dependency, ARM64 availability, backup, client maturity, projection integration and homelab effort are assessed.

### Q6.3 Decision

PostgreSQL is the default hypothesis. An external store is selected only if it provides verifiable added value. Exactly one production provider is completed for the MVP.

**Gate G6:** ADR with decision, risks and rejected alternatives.

## 11. Phase F7: Production Provider

### F7.1 Schema and Migrations

At least logical:

```text
event_streams
events
snapshots optional
projection_checkpoints
```

Schema, indexes and constraints are derived from the contract requirements, not from the convenience of the ORM.

### F7.2 Append

- a transaction;
- Create or block stream;
- Check expected version atomically;
- Write batch of events;
- assign global positions;
-commit;
- only then visible as committed.

### F7.3 Tests

- entire contract suite;
- Test containers on Linux;
- true parallelism with multiple connections;
- Process termination and rollback;
- Migration from empty and previous schema version;
- Backup/Restore smoke test;
- long streams and large payloads within sensible limits.

**Gate G7:** Enable production persistence.

## 12. Phase F8: Projections and Recovery

### F8.1 Feed and Checkpoint

- monotone cursor;
- Checkpoint per projection name and version;
- Event application and checkpoint update in the same read store transaction where possible;
- idempotent repetition;
- no OFFSET-based navigation.

### F8.2 Error handling

- affected projection stops or goes into visible retry state;
- other projections run independently;
- Domain commit remains valid;
- Admin can trigger retry and rebuild;
- Poison event is diagnosable, not silently skipped.

### F8.3 Rebuild

- Discard Read Model;
- reset checkpoint;
- replay from position zero;
- Progress visible;
- Compare result against incremental projection.

**Gate G8:** Enable CQRS basis.

## 13. Phase F9: Snapshots

F9 only starts when there is measured need or when the existing framework API needs to be kept consistent without much additional effort.

- Snapshot references stream version;
- Snapshot format versioned;
- incompatible snapshot is discarded;
- full replay remains possible;
- Snapshot error does not change event commit;
- Equivalence test replay against snapshot plus residual events.

## 14. Phase F10: Packaging and release

### F10.1 Package quality

- Package dependencies minimal and correct;
- README and package description;
- License and repository metadata;
- Symbols and SourceLink;
- XML ​​documentation;
- no examples, secrets or build residues in the package;
- `dotnet nuget verify`, if signing is used;
- Consumer smoke test in empty project.

### F10.2 Versioning

Recommendation:

```text
0.x.y-alpha.N    modernization and API exploration
0.x.y-rc.N       Playground-compatible release candidate
1.0.0            only after the public API has stabilized
```

If the framework has already published stable versions, SemVer will instead be continued compatible with the existing history. An artificial return to `0.x` would then be wrong.

### F10.3 Feeds

- local folder feed for immediate development;
- private or GitHub-based feed for reproducible pre-releases;
- NuGet.org only if consciously published publicly;
- Playground pins a concrete version and never `*`.

### F10.4 Release Gate

- Unit, contract, integration and recovery suites green;
- Package Consumer Smoke Test green;
- Release Notes and Migration Guide;
- known limitations;
- tagged commit;
- express release `READY FOR PLAYGROUND`.

## 15. Codex cutting

Each sub-stage is formulated as a separate task. An order contains a maximum of one main reason for changes.

Example order of the first orders:

```text
1. F0.1 and F0.3: examine the baseline without changing anything
2. Add the dev container and reproducible commands
3. Migrate the target to .NET 10 without a major package update
4. Introduce Central Package Management
5. Classify nullable findings
6. Harden nullable handling in one package
7. Establish package and consumer smoke tests
```

The complete order formulations are only created after reviewing the previous results.

## 16. Critical Review Points

- Is Core kept free of Npgsql, EF Core, ASP.NET and hosting?
- Do domain events remain free of infrastructure metadata?
- Is Expected Version really atomic in the provider?
- Isn't in-memory more generous than production?
- Can every read model be completely rebuilt?
- Is event evolution possible without CLR name binding?
- Are package boundaries useful or just historical?
- Is a breaking change more honest than a misleading compatibility layer?

## 17. Graduation

The framework modernization is complete when not only the code looks modern, but an external consumer project successfully uses the packaged artifacts with real persistence, concurrency, projection and recovery semantics.
