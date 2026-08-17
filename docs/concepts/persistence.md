# Repository and persistence providers

## Repository responsibility

`EventSourcingRepository` orchestrates aggregate rehydration and commit. It owns no database-specific behavior. It combines:

- `IEventStoreProvider` for atomic envelope persistence and reads;
- `IEventCodec` for stable event serialization;
- optional snapshot store and codec configured as a pair.

One repository instance can persist all aggregate types because stream identity includes the aggregate type.

## Event-store contract

`IEventStoreProvider` exposes three operations:

- `AppendAsync`: atomically append a contiguous batch with an expected version;
- `ReadStreamAsync`: read an aggregate stream from a stream version;
- `ReadCommittedFeedAsync`: read globally committed envelopes after a position.

Provider implementations must preserve:

- unique event IDs;
- stable per-stream order;
- monotonic committed global positions;
- all-or-nothing batch append;
- concurrency conflict classification;
- cancellation without wrapping `OperationCanceledException`.

## Available providers

| Provider | Intended use | Data access |
|---|---|---|
| `InMemoryEventStoreProvider` | tests, samples, local development | thread-safe memory structures |
| `PostgreSqlEventStoreProvider` | production PostgreSQL | Npgsql and explicit SQL |
| `SqlServerEventStoreProvider` | production SQL Server | Microsoft.Data.SqlClient and explicit SQL |

EF Core is deliberately absent from the event-store hot path.

## Migrations

The relational packages include idempotent versioned migrators. Run migration as a deployment step or controlled startup task before creating providers. Use the same provider options for migration, event store, snapshots, and checkpoints.

Do not let every horizontally scaled application instance race to perform privileged schema management in production. Prefer one deployment job with restricted migration credentials.

## Aggregate types

The repository currently uses the aggregate CLR full name as its aggregate-type discriminator. Treat namespace/type moves of persisted aggregates as storage migrations. Stable event keys protect event identity, but they do not automatically alias aggregate stream types.

## Provider boundaries

Event append does not include:

- snapshot writes;
- projection updates;
- external message publication;
- cross-stream uniqueness enforcement;
- arbitrary application database work.

Those operations have distinct consistency and recovery mechanisms. Keeping them explicit prevents a generic provider from promising a transaction it cannot safely deliver.
