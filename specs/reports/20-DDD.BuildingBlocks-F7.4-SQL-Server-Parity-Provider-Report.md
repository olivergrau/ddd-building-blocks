# F7.4 Outcome Report: SQL Server Parity Provider

## Status

The optional clean-room SQL Server parity provider is implemented and verified after Gate G7. It is awaiting explicit follow-up review.

## Provider boundary

`DDD.BuildingBlocks.MSSQLPackage` implements the current envelope-based `IEventStoreProvider` contract with Microsoft.Data.SqlClient 7.0.2 and explicit parameterized SQL. EF Core is not referenced or used. No code, schema, CLR-type persistence, or provider-side aggregate reflection from the removed legacy provider was reused.

`SqlServerEventStoreMigrator` creates an idempotent versioned schema containing:

- `SchemaVersions` for migration state;
- `EventStreams` for stream identity and current version;
- `EventStoreState` for transactional global-position allocation;
- `Events` for constrained envelope metadata and JSON payloads;
- `EventBatchType`, a table type used for set-based batch append.

## Append and concurrency semantics

Each append uses one `READ COMMITTED` transaction. The provider locks the stream key range with `UPDLOCK, HOLDLOCK`, verifies the expected version, reserves global positions under the singleton allocator lock, inserts the entire batch through one table-valued parameter, advances the stream version, and commits.

The allocator provides the same checkpoint-safe total commit order as PostgreSQL. Failed or interrupted transactions roll back stream creation, events, and reserved positions together. Unique constraints protect Event IDs and stream versions. SQL Server error numbers are mapped to the shared transient/permanent provider classification; cancellation remains unwrapped.

## Operational evidence

The SQL Server 2025 CU7 Testcontainer suite verifies:

- all 11 normative provider-contract cases unchanged;
- 50 parallel stream appends over pooled connections with a contiguous committed feed;
- a 100-event atomic TVP batch with 64-KiB payloads per event;
- duplicate-event rollback without a global-position gap;
- forced session termination through `KILL` with complete transactional rollback;
- idempotent migration from an empty schema;
- native `BACKUP DATABASE` and `RESTORE DATABASE`, followed by continued append and feed reads.

Snapshot activation remains optional, but the separate SQL Server snapshot provider is a mandatory F9 deliverable. Full event replay remains mandatory.

## Verification

- Restore: successful; no known vulnerable direct or transitive packages.
- Solution build: successful with zero warnings and zero errors.
- SQL Server integration: 16/16 passed.
- PostgreSQL integration: 16/16 passed in the same full run.
- Core: 64/64 passed; DevelopmentPackage integration: 28/28 passed.
- RocketLaunch Application: 23/23; ReadModel: 27/27.
- RocketLaunch Domain: 21/21; LunarOps Domain: 53/53.
- RocketLaunch API: 6/7 passed; the failure is the previously accepted asynchronous projection timing race.
- Package build: seven active `.nupkg` and seven `.snupkg` files, including both relational event-store providers.

## Review boundary

Review accepts SQL Server as a supported production parity provider, its independent schema and migration baseline, TVP batch append, checkpoint-safe position allocation, and operational evidence. This follow-up does not begin F8 or F9.
