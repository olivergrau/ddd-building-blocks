# Recovery and projection rebuild

This guide covers projection failures and rebuilding read models. Database disaster recovery is addressed in [Production operations](operations.md).

## Diagnose projection state

Read the projection checkpoint and record:

- projection name and version;
- `LastProcessedPosition`;
- status;
- failed position;
- last error;
- update timestamp.

Then inspect the committed envelope at the failed position and the deployed event registry/upcaster set. Avoid editing checkpoints before understanding the failure.

## Poison-event recovery

1. Pause the affected projection worker.
2. Confirm other projections continue independently.
3. Reproduce the failure in an isolated database or integration test.
4. Fix code, read-model schema, event compatibility, or data.
5. Deploy the fix.
6. Call `RetryBatchAsync` for the same projection.
7. Confirm the failed position commits and status returns to running.
8. Resume normal batches and monitor lag.

Never skip the position merely to make the checkpoint move. A partial read model is harder to detect than a stopped one.

## Full rebuild procedure

1. Decide whether queries can tolerate an empty/partial read model.
2. Prefer a shadow read-model schema/table set for zero-downtime rebuilds.
3. Stop normal processing for the target projection identity.
4. Call `RebuildAsync` with a reset callback that uses the supplied transaction.
5. Replay bounded batches from global position zero.
6. expose progress and estimated remaining work;
7. validate counts, invariants, and representative queries;
8. atomically switch consumers to the rebuilt model;
9. retain the old model until rollback is no longer needed.

## Projection versioning

For a materially different model, create `ProjectionKey("name", 2)` rather than reusing version 1 blindly. This keeps checkpoint history explicit and permits side-by-side build where table design allows it.

## Gap failures

Checkpoint stores reject a feed gap. A gap indicates that the caller supplied noncontiguous committed positions, the event store/feed is damaged, or checkpoint state was edited incorrectly. Stop and investigate; do not advance past the gap.

## Restored databases

After restoring an event database:

- verify stream/event counts and final versions;
- verify global positions are unique and ordered;
- verify the global-position allocator is not behind stored data;
- discard snapshots if their consistency is uncertain;
- rebuild at least one representative projection;
- compare results with pre-backup expectations.

## Recovery evidence

Record the backup identifier, restored commit/version, migration version, event counts, last global position, projection versions, validation queries, timings, and operator. Recovery that is not rehearsed and evidenced is only a hypothesis.
