# Changes in DDD.BuildingBlocks

## 2.1.0

DDD.BuildingBlocks 2.1 adds explicit persistence identities for aggregate types and complete repository commit metadata. The release is additive and keeps the 2.0 event-store, provider, projection, and snapshot contracts intact.

### Stable aggregate type keys

- Added `IAggregateTypeRegistry` and the thread-safe `AggregateTypeRegistry`.
- Aggregate CLR types can be registered under permanent logical keys such as `playground.session` and `playground.persona`.
- Stream reads, appends, and snapshots use the registered key, so namespace, assembly, and CLR type-name refactorings no longer change stream identity.
- Duplicate logical keys, duplicate CLR registrations, and unregistered aggregate types fail explicitly.
- Added `EventSourcingRepository.Create(...)` for explicitly registered aggregate types.
- The existing constructor retains CLR-full-name resolution as a compatibility path for 2.0 consumers.

### Commit metadata

- Added immutable `EventCommitMetadata` with `CorrelationId`, `CausationId`, `CommandId`, `Actor`, and `TurnId`.
- Added an `IEventSourcingRepository.SaveAsync` overload that writes the commit context to every event envelope produced by one aggregate operation.
- An omitted correlation ID preserves the correlation ID already captured by each domain event.
- `LoggingRepositoryDecorator` forwards the complete metadata unchanged.
- Third-party repository implementations that have not adopted the new overload fail explicitly rather than silently discarding metadata.
- Commit metadata supports tracing and application-level idempotency but does not replace a command inbox or operational turn store.

### Documentation and verification

- Updated the quickstart, event-contract guidance, and composition-root example.
- Added tests for stable aggregate persistence and reload, complete metadata propagation, duplicate registration, and missing registration.
- Verified all framework and non-API example suites. The known timing-sensitive legacy RocketLaunch API projection race remains unchanged.

## 2.0.0

DDD.BuildingBlocks 2.0 is a deliberate breaking modernization of the 1.x codebase. It keeps the framework focused on event-sourced domain aggregates and replaces persistence, serialization, dispatch, projection, and snapshot infrastructure with explicit versioned contracts.

Applications must treat the upgrade as a coordinated migration. Do not mix 1.x event-storage or snapshot implementations with 2.0 repositories.

## Platform and build baseline

- The target framework is .NET 10 LTS and the repository pins SDK `10.0.400`.
- Nullable reference types, .NET analyzers, warnings-as-errors, deterministic builds, Central Package Management, XML documentation, Source Link, and symbol packages are enabled.
- All asynchronous framework I/O, persistence, command dispatch, and event dispatch contracts require a `CancellationToken`.
- The seven active framework packages are built and versioned together as `2.0.0`.

## Domain model

- `AggregateRoot<TKey>` remains the only aggregate-root base type. There is no conventional or state-storage aggregate root.
- `RaiseEvent` is the normal API for recording a new domain event. The existing `ApplyEvent` entry point remains only as a compatibility alias.
- Event replay is separated from event creation through protected replay boundaries.
- Uncommitted-event collections are exposed as immutable snapshots.
- Entity equality now respects runtime types and value-object equality is null-safe.
- Stream versions use `long` throughout aggregates, events, snapshots, repositories, providers, and storage schemas instead of `int`.

## Event contracts and serialization

- Events are stored in immutable `EventEnvelope` values rather than provider-specific CLR object graphs.
- Every persisted event type requires an explicit stable key in `EventTypeRegistry`; CLR assembly-qualified names are not persistence identities.
- `IEventCodec` is the serialization boundary. `SystemTextJsonEventCodec` is the default implementation.
- Event schema versions are explicit. Sequential JSON upcasters transform historical payloads before deserialization.
- Missing registrations, duplicate keys, invalid upcaster chains, corrupt payloads, and incompatible schemas are classified failures.
- Event and aggregate identifiers used by examples were normalized to stable string representations.

## Repository and event-store providers

- `IEventStorageProvider` was removed and replaced by envelope-based `IEventStoreProvider`.
- `IEventStoreProvider.AppendAsync` performs atomic expected-version appends and returns assigned stream and global positions.
- Stream reads use stable aggregate type plus stream identity and a zero-based `long` stream version.
- The committed feed is ordered by monotonic global position and supports projection recovery without line or SQL offsets.
- Optimistic concurrency, duplicate event IDs, missing streams, already-existing streams, provider failures, and cancellation have explicit semantics.
- `FileInMemoryEventStorageProvider` and `PureInMemoryEventStorageProvider` were removed. `InMemoryEventStoreProvider` is the normative development implementation.
- The old reflection-driven unique-property mechanism and `UniqueDomainPropertyAttribute` were removed. Cross-stream uniqueness belongs in an explicit domain/application or projection design.

## PostgreSQL provider

- `DDD.BuildingBlocks.PostgreSQLPackage` is a new production provider using Npgsql and explicit parameterized SQL.
- Versioned idempotent migrations create event streams, events, global-position allocation, projection checkpoints, and snapshots.
- Expected-version append uses transactional stream locking and batched inserts.
- Global positions remain monotonic and rollback-safe.
- Projection checkpoint and snapshot stores are included.
- EF Core is not used in the event-store or projection-checkpoint hot path.

## SQL Server provider

- The previous SQL Server implementation and its CLR-type-bound schema were removed rather than adapted.
- `DDD.BuildingBlocks.MSSQLPackage` now provides a clean-room parity adapter using Microsoft.Data.SqlClient and explicit parameterized SQL.
- Atomic expected-version append uses SQL Server locking and table-valued event batches.
- Versioned migrations, committed-feed reads, projection checkpoints, and versioned snapshots match the PostgreSQL semantics.
- The old `DDL.sql`, provider settings, reflection services, and event-processing background worker were removed.
- Existing 1.x SQL Server databases require an explicit export/transform/import migration; the 2.0 migrator does not reinterpret legacy rows automatically.

## Snapshots

- `ISnapshotStorageProvider` was removed and replaced by `ISnapshotStoreProvider`.
- Snapshots are persisted as immutable `SnapshotEnvelope` values with stream version, stable snapshot key, schema version, creation time, and JSON payload.
- `ISnapshotCodec`, `SnapshotTypeRegistry`, and `SystemTextJsonSnapshotCodec` provide explicit serialization.
- In-Memory, PostgreSQL, and SQL Server snapshot providers implement the same normative contract.
- Unknown, malformed, or schema-incompatible snapshots are discarded and rehydration falls back to full event replay.
- Snapshot creation and persistence occur after a successful event append. A snapshot failure cannot turn the event commit into a failure.
- The old binary/file-dump snapshot provider was removed.

## Projections and recovery

- Core provides stable `ProjectionKey`, checkpoint, transaction-context, runner, retry, reset, and rebuild contracts.
- Each projection owns an independent versioned checkpoint over committed global positions.
- Projection updates and checkpoint advancement execute in the same provider transaction.
- Failed/poison events are visible and stop only the affected projection.
- Retry is explicit and successful retry clears the failure state without skipping the event.
- Rebuild atomically resets read-model state and checkpoint and then replays from global position zero.
- In-Memory, PostgreSQL, and SQL Server checkpoint stores pass the same contract suite.

## Dependency injection and dispatch

- `ServiceLocator`, `IDependencyResolver`, `DefaultCommandProcessor`, and their compatibility APIs were removed.
- Command and domain-event dispatch use explicit Microsoft.Extensions.DependencyInjection registrations.
- Startup validation rejects duplicate or missing handlers.
- Each dispatch creates an independent dependency-injection scope so scoped handler dependencies are safe.
- Cancellation exceptions remain unwrapped.

## Development and test infrastructure

- Provider semantics are specified once in reusable event-store, projection-checkpoint, and snapshot contract suites.
- PostgreSQL and SQL Server integration tests run against real Testcontainers databases.
- Operational tests cover concurrent appends, large batches, transaction rollback, migration idempotency, and database backup/restore.
- RocketLaunch and LunarOps remain executable examples and provide event-registry and snapshot-registry composition examples.

## Package changes

- All active packages ship as `2.0.0` with MIT license metadata, repository metadata, embedded package README, Source Link, XML documentation, `.snupkg` symbols, and deterministic builds.
- `DDD.BuildingBlocks.PostgreSQLPackage` is newly added.
- `DDD.BuildingBlocks.MSSQLPackage` has new public provider types and is not API-compatible with its 1.x implementation.
- Package files are distributed as GitHub Release assets rather than being pushed to a public NuGet registry.

## Required consumer migration

At minimum, a 1.x consumer must:

1. move to .NET 10 and reference one consistent set of `2.0.0` packages;
2. register every persisted event with a stable key and schema version;
3. configure an `IEventCodec` and migrate repository construction to `IEventStoreProvider`;
4. pass cancellation tokens to all asynchronous framework calls;
5. replace service-locator/default-processor usage with explicit DI dispatch registration;
6. migrate stream versions and database columns from `int` to `long`/`BIGINT`;
7. deploy the new PostgreSQL or SQL Server schema and explicitly migrate any legacy event data;
8. configure snapshot registry/codec/store together when snapshots are enabled;
9. create projection checkpoints and validate rebuild behavior before switching production read models;
10. run the consumer's event-history, concurrency, recovery, and projection-equivalence tests before production cutover.

See [MIGRATION-2.0.md](MIGRATION-2.0.md) for an ordered migration procedure and [RELEASE_NOTES-2.0.0.md](RELEASE_NOTES-2.0.0.md) for the release summary.
