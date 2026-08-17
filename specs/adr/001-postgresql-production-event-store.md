# ADR 001: PostgreSQL as the Production Event Store

- Status: Accepted
- Decision date: 2026-08-17
- Gates: G6 accepted; implementation reviewed at G7

## Context

DDD.BuildingBlocks requires atomic expected-version append, immutable event envelopes, ordered stream reads, a checkpoint-safe committed feed, reproducible migrations, Linux and ARM64 operation, and conventional backup and restore procedures. F5 established `IEventStoreProvider` and its provider-contract suite as the normative boundary.

The candidates were a dedicated PostgreSQL adapter, adaptation of the removed CLR-type-bound relational implementation, and an external event-store product.

## Decision

PostgreSQL is the reference production event store. The adapter uses Npgsql and explicit parameterized SQL. EF Core is not used in append or read paths.

The schema is derived from provider invariants rather than an ORM model. Stream rows provide atomic optimistic concurrency. Events are appended as one set-based batch. Database constraints enforce stream version and Event ID uniqueness. A transactional singleton allocator assigns global positions in commit order so feed checkpoints cannot skip a transaction that commits later with a lower position.

The first implementation targets PostgreSQL 18 and is tested through Testcontainers. Npgsql uses one shared, thread-safe `NpgsqlDataSource` per application/database boundary.

Snapshot storage and projection checkpoints remain separate persistence contracts and do not participate implicitly in the event append transaction. Snapshot activation is optional for consumers, but provider implementations are mandatory framework deliverables.

## Consequences

- Applications can operate the event store with the same PostgreSQL platform used for projections and operational relational data.
- The append path has no ORM tracking, generated SQL, or hidden retry behavior.
- Global feed position allocation introduces a short serialization boundary. This is required for a checkpoint-safe total commit order and must be monitored under high write load.
- Horizontal sharding would require an explicit replacement for the single global feed order.
- Schema migrations, database privileges, backup, restore, vacuuming, monitoring, and capacity planning remain operational responsibilities.
- The SQL Server provider was subsequently delivered as a clean-room parity implementation and passes the same provider contracts after PostgreSQL passed Gate G7.

## Rejected alternatives

### External event-store product

Rejected for the current product because it adds deployment, backup, client, monitoring, and homelab dependencies without a required capability absent from PostgreSQL. The framework contract does not prevent a future third-party adapter if concrete requirements change.

### Reusing the former MSSQL implementation

Rejected because it persisted CLR-bound event objects and implemented provider-side aggregate reflection semantics that conflict with the immutable envelope and normative F5 contracts.

### EF Core in the event-store hot path

Rejected because the small fixed schema benefits from explicit SQL, set-based batch append, predictable locking, and direct database error classification. EF Core remains valid outside this hot path where its modeling benefits justify it.
