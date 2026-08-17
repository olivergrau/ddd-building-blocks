# F9 Outcome Report: Versioned Snapshots

## Status

F9 is implemented, verified, and approved at Gate G9.

## Contract and format

Core now provides an immutable `SnapshotEnvelope`, the `ISnapshotStoreProvider` persistence boundary, and an explicit `ISnapshotCodec`. `SnapshotTypeRegistry` maps stable snapshot keys to CLR snapshot types and current positive schema versions. `SystemTextJsonSnapshotCodec` stores snapshots as JSON without persisting assembly-qualified CLR type names.

Snapshot envelopes carry stream identity, aggregate type, stream version, stable snapshot type, schema version, creation time, and an immutable JSON payload. An unknown snapshot key, incompatible schema version, malformed payload, or payload whose identity does not match its envelope is discarded. Repository rehydration then performs a full event replay.

Snapshot use remains optional for a consumer. When enabled, the store and codec must be configured together so that incomplete startup configuration cannot silently disable the optimization.

## Provider implementations

The Development package supplies a thread-safe in-memory provider. The PostgreSQL and SQL Server packages supply production adapters backed by versioned schema migration 3. Both relational providers use explicit parameterized SQL, select the latest snapshot at or below an optional stream version, and idempotently replace an existing envelope at the same stream version.

The SQL Server upsert holds an update-range lock inside an explicit transaction. PostgreSQL uses `ON CONFLICT`. No provider uses EF Core in the persistence hot path.

All three implementations pass the same six-case `SnapshotStoreProviderContract`, covering configured frequency, latest and bounded reads, aggregate-type isolation, idempotent replacement, and cancellation propagation.

## Commit and recovery semantics

`EventSourcingRepository` appends events first and immediately marks the aggregate changes committed after the append succeeds. Snapshot creation and persistence happen afterward as a disposable optimization. Any snapshot codec or provider failure therefore cannot report the successful event commit as failed or leave those events marked as uncommitted.

Existing integration scenarios prove equivalence between full replay and snapshot plus residual-event replay over different snapshot and save frequencies. Unit tests additionally prove JSON codec roundtrip, incompatible-snapshot discard, incomplete-configuration rejection, and event-commit isolation from a forced snapshot-store failure.

The obsolete CLR-object `ISnapshotStorageProvider` and file-dump `InMemorySnapshotStorageProvider` were removed. The RocketLaunch example now uses the envelope contract and no longer exposes obsolete snapshot-file configuration.

## Verification

- Solution restore: successful.
- Solution build: successful with zero warnings and zero errors.
- Core: 73/73 passed.
- DevelopmentPackage integration: 41/41 passed.
- PostgreSQL integration: 30/30 passed in a real Testcontainer.
- SQL Server integration: 30/30 passed in a real Testcontainer.
- RocketLaunch Application: 23/23; ReadModel: 27/27.
- RocketLaunch Domain: 21/21; LunarOps Domain: 53/53.
- RocketLaunch API: 5/7 passed; the two failures are the previously accepted asynchronous legacy projection timing race and are unrelated to F9.

## Gate G9 review

Gate G9 accepts the versioned disposable snapshot format, mandatory In-Memory/PostgreSQL/SQL Server implementations, fallback behavior, and commit-isolation semantics. Gate G9 was explicitly approved before F10 began.
