# F7 Outcome Report: PostgreSQL Production Provider

## Status

F6 selected PostgreSQL and was accepted. F7 was implemented, verified, and accepted at Gate G7. The optional SQL Server parity provider was implemented afterward and is documented separately.

## Production adapter

`DDD.BuildingBlocks.PostgreSQLPackage` implements `IEventStoreProvider` with Npgsql 10.0.3 and explicit parameterized SQL. EF Core is not referenced or used.

The provider accepts a shared `NpgsqlDataSource` and validated schema options. `PostgreSqlEventStoreMigrator` creates an idempotent versioned initial schema containing:

- `schema_versions` for migration state;
- `event_streams` for stream identity and current version;
- `event_store_state` for transactional global-position allocation;
- `events` for immutable envelope storage as constrained relational metadata plus `jsonb` payload.

## Append and concurrency semantics

Each append uses one `READ COMMITTED` transaction:

1. create a new stream or lock the existing stream row;
2. compare the expected version while holding that lock;
3. reserve a contiguous global-position range under the transactional allocator lock;
4. insert the complete event batch through one set-based `unnest` statement;
5. advance the stream version;
6. commit.

PostgreSQL sequences were deliberately not used for the committed feed. Sequence values do not represent commit order and could allow a projection checkpoint to skip a lower position from a transaction that commits later. The allocator lock makes committed feed positions checkpoint-safe. Failed or interrupted transactions roll back the stream, events, and allocator together.

Unique constraints protect Event IDs and `(aggregate_type, stream_id, stream_version)`. Stream reads and the committed feed are ordered and indexed. Cancellation remains unwrapped; Npgsql failures are classified as transient or permanent provider errors.

## Operational evidence

The PostgreSQL 18.1 Alpine Testcontainer suite verifies:

- all 11 normative provider-contract cases unchanged;
- 50 genuinely parallel stream appends over pooled connections with a contiguous committed feed;
- a 100-event atomic batch with 64-KiB payloads per event;
- duplicate-event rollback without a global-position gap;
- forced backend termination with complete transactional rollback;
- idempotent migration from an empty schema;
- schema-level `pg_dump` and `pg_restore`, followed by continued append and feed reads.

Snapshot activation remains optional, but the separate PostgreSQL snapshot provider is a mandatory F9 deliverable. Full event replay remains mandatory, and snapshot persistence must not change event-commit success when it fails.

## Verification

- Restore: successful; no known vulnerable direct or transitive packages.
- Solution build: successful with zero warnings and zero errors.
- PostgreSQL integration: 16/16 passed.
- Core: 64/64 passed.
- DevelopmentPackage integration: 28/28 passed.
- RocketLaunch Application: 23/23 passed; ReadModel: 27/27 passed.
- RocketLaunch Domain: 21/21 passed; LunarOps Domain: 53/53 passed.
- RocketLaunch API: 5/7 passed; both failures are the previously accepted asynchronous projection timing race.
- Package build: six active `.nupkg` and six `.snupkg` files, including the PostgreSQL provider.

## Gate G7 review

Gate G7 accepted PostgreSQL as production persistence, the schema and migration baseline, the checkpoint-safe global-position serialization boundary, and the operational evidence above.
