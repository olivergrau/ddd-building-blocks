# Terminology

## Aggregate root

The single entry point to a domain consistency boundary. In this framework it derives from `AggregateRoot<TKey>` and is event sourced.

## Aggregate type

The discriminator stored with a stream. DDD.BuildingBlocks currently derives it from the aggregate CLR full name.

## Stream ID

The stable string representation of an aggregate identifier.

## Stream version

Zero-based `long` position of an event within one aggregate stream. `-1` represents no stream.

## Expected version

The last committed stream version a writer observed. An append succeeds only if current storage version matches it.

## Domain event

An immutable fact that occurred inside the bounded context and changes aggregate state when applied.

## Event key

A stable string identifying a persisted event contract independently of CLR type/namespace names.

## Schema version

A positive integer describing the JSON representation of an event or snapshot contract.

## Event envelope

Immutable storage value combining stream/global metadata with a JSON event payload.

## Codec

Boundary that encodes domain events/snapshots to envelopes and decodes envelopes to current CLR objects.

## Upcaster

A deterministic JSON transformation from one historical event schema version to exactly the next version.

## Global position

A monotonic committed-feed cursor across all aggregate streams. It is not a stream version.

## Projection

A consumer of committed events that materializes a query/read model.

## Checkpoint

Durable per-projection record of the last successfully applied global position and failure state.

## Poison event

An envelope that repeatedly fails a specific projection. It remains visible at the failed position until repaired and retried.

## Rebuild

Reset a projection's read model and checkpoint, then replay the committed feed from position zero.

## Snapshot

A disposable cached aggregate state at a stream version, used to reduce replay cost.

## Uncommitted event

A new event already applied to an in-memory aggregate but not yet successfully appended to its event store.

## Correlation ID

Identifier grouping operations/events that belong to one broader interaction or workflow.

## Causation ID

Identifier of the message/operation that directly caused another message or event.

## Integration event

An external contract published between bounded contexts or systems after the domain commit. It need not match the internal persisted domain event.
