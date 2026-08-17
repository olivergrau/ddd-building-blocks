# Projections and recovery

## Why projections exist

Aggregate streams are optimized for enforcing domain invariants, not query shapes. A projection consumes the committed event feed and builds a read model suited to queries, reports, APIs, or integration output.

## Projection identity

`ProjectionKey` combines a stable projection name with a positive version. Increment the version when the read-model interpretation changes incompatibly and a rebuild is required.

Each projection has an independent checkpoint. One failed projection does not stop another projection from advancing.

## Transaction boundary

`IProjectionCheckpointStore.ProcessAsync` owns the transaction. It passes a `ProjectionTransactionContext` to the projection callback. For relational providers, the context exposes the provider connection and transaction.

The projection must write its read model through that connection and transaction. The checkpoint advances only if the callback commits successfully.

```text
read next committed envelope
  -> begin checkpoint transaction
  -> apply read-model update
  -> advance checkpoint
  -> commit both
```

This provides idempotent redelivery: an already committed position is ignored, while a rolled-back read-model write has no advanced checkpoint.

## Runner behavior

`ProjectionRunner.RunBatchAsync` reads after the last checkpoint, processes a bounded batch, and returns feed count, applied count, final position, and whether more work may exist.

If applying an envelope fails, the runner records the failed position and diagnostic error using a separate failure update, then rethrows. The poison event remains visible; it is not skipped.

## Retry

Fix the underlying code or data, then call `RetryBatchAsync`. Because the checkpoint did not advance, the same event is processed again. A successful transaction clears the failed state.

## Rebuild

`RebuildAsync` invokes a read-model reset callback and resets the checkpoint in the same checkpoint-store transaction. Subsequent batches replay from global position zero.

A rebuild should be observable and bounded. Run batches until `MayHaveMore` is false and expose progress based on checkpoint/global position rather than line offsets.

## Delivery expectation

Projections are at-least-once at the processing boundary and exactly-once in effect only when read-model mutation and checkpoint advancement share the provided transaction and mutations are deterministic.
