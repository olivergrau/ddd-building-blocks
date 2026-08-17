# F8 Outcome Report: Projections and Recovery

## Status

F8 is implemented, verified, and approved at Gate G8.

## Projection contracts

Core now provides:

- `ProjectionKey` for a stable projection name and positive schema/version number;
- `ProjectionCheckpoint` with last processed position, lifecycle status, failed position, diagnostic error, and update timestamp;
- `IEventProjection` for envelope application;
- `IProjectionCheckpointStore` for transactional process, failure, and reset operations;
- `ProjectionTransactionContext` exposing provider-neutral ADO.NET connection and transaction boundaries;
- `ProjectionRunner` for position-based batches, explicit retry, and rebuild reset;
- `ProjectionRunResult` for batch progress and remaining-work visibility.

Feed navigation uses the exclusive global-position cursor and never uses an SQL or line offset.

## Transaction and idempotency semantics

`ProcessAsync` owns the projection transaction. It locks or creates the checkpoint, ignores positions already committed, rejects feed gaps, invokes the read-model callback, and advances the checkpoint in the same transaction. PostgreSQL callbacks receive the active Npgsql connection and transaction; SQL Server callbacks receive the active SqlClient connection and transaction. A callback failure rolls back both read-model changes and checkpoint advancement.

In-Memory, PostgreSQL, and SQL Server implement the same seven-case `ProjectionCheckpointStoreContract`:

- initial checkpoint;
- atomic application and advancement;
- idempotent redelivery;
- no advancement after handler failure;
- visible poison-event state;
- gap rejection;
- atomic read-model reset and checkpoint reset.

## Failure, retry, and rebuild

`ProjectionRunner` records a failed position and full diagnostic error without affecting the domain commit or another projection's checkpoint. A caller triggers retry explicitly through `RetryBatchAsync`. Successful retry clears the failure state. `RebuildAsync` resets the read model and checkpoint through one checkpoint-store transaction, after which replay starts at global position zero.

Runner tests prove independent projection progress, poison-event retry without skipping, bounded position batches, and equality between incremental output and complete rebuild output.

Provider-specific integration tests additionally execute real read-model SQL inside the checkpoint transaction and prove that a deliberately failing callback leaves neither its row nor its checkpoint committed.

## Snapshot delivery clarification

Snapshot use remains optional for consumers because full event replay is always valid. Snapshot-provider availability is not optional framework scope: F9 must deliver a normative snapshot contract and implementations for In-Memory, PostgreSQL, and SQL Server.

## Verification

- Solution build: successful with zero warnings and zero errors.
- Core: 68/68 passed.
- DevelopmentPackage integration: 35/35 passed.
- PostgreSQL integration: 24/24 passed.
- SQL Server integration: 24/24 passed.
- RocketLaunch Application: 23/23; ReadModel: 27/27.
- RocketLaunch Domain: 21/21; LunarOps Domain: 53/53.
- RocketLaunch API: 5/7 passed; both failures are manifestations of the previously accepted asynchronous legacy projection timing race. The example has not yet migrated from its legacy publishing table to the new F8 runner.
- Package build: seven active `.nupkg` and seven `.snupkg` files.
- Vulnerability scan: no known vulnerable direct or transitive packages.

## Gate G8 review

Gate G8 accepts the projection identity, checkpoint, transaction, failure, retry, and rebuild contracts plus all three checkpoint-store implementations as the CQRS recovery baseline. Gate G8 was explicitly approved before F9 began.
