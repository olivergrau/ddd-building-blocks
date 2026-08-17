# Specifications and Modernization Documentation

This folder separates governing documents from evidence produced during implementation.

## Current Architectural Baseline

- DDD.BuildingBlocks remains focused on event-sourced domain aggregates.
- `AggregateRoot<TKey>` remains the single aggregate-root type and therefore keeps its existing name.
- No `ConventionalAggregateRoot<TKey>`, `PlainAggregateRoot<TKey>`, or marker-only `IAggregateRoot<TKey>` is introduced.
- Relational persistence remains valid for projections and non-aggregate technical or operational state.
- New event persistence contracts use stable event keys, an immutable envelope, sequential schema upcasting, and `long` stream versions.
- PostgreSQL with Npgsql and explicit SQL is the reference production event-store provider; the SQL Server parity provider uses Microsoft.Data.SqlClient and explicit SQL. EF Core is excluded from both hot paths.
- Snapshot activation is optional for consumers; the versioned snapshot contract and providers for In-Memory, PostgreSQL, and SQL Server are implemented in F9.
- A user-facing documentation package with a quickstart, concepts, and tutorials/guides based on the executable examples is scheduled after modernization.

## Analysis and Planning

1. [08A – Framework Analysis](08A-DDD.BuildingBlocks-Framework-Analysis.md)
2. [08B – Modernization Plan](08B-DDD.BuildingBlocks-Modernization-Plan.md)
3. [08C – Usage and Release Gate](08C-DDD.BuildingBlocks-Usage-and-Release-Gate.md)
4. [10 – Detailed Implementation Plan](10-DDD.BuildingBlocks-Detailed-Implementation-Plan.md)
5. [11 – Linux, VS Code, and Dev Container Development Environment](11-Linux-VS-Code-and-Devcontainer-Development-Environment.md)
6. [12 – Two-Repository and Packaging Strategy](12-Two-Repository-and-Packaging-Strategy.md)

## Architecture Decisions

- [ADR 001 – PostgreSQL as the Production Event Store](adr/001-postgresql-production-event-store.md)

## Preparatory Working Document

- [Preparation Plan Before Implementation](Preparation-Plan-Before-Implementation.md)

## Implementation Reports and Gate Evidence

- [F0 – Baseline Report](reports/13-DDD.BuildingBlocks-F0-Baseline-Report.md)
- [F1 – Outcome Report and Gate G1](reports/14-DDD.BuildingBlocks-F1-Outcome-Report.md)
- [F2 – Tactical Domain Primitives and Gate G2](reports/15-DDD.BuildingBlocks-F2-Tactical-Domain-Primitives-Report.md)
- [F3 – Event Contracts and Evolution and Gate G3](reports/16-DDD.BuildingBlocks-F3-Event-Contracts-and-Evolution-Report.md)
- [F4 – Async, Cancellation, Error, and DI and Gate G4](reports/17-DDD.BuildingBlocks-F4-Async-Cancellation-Error-and-DI-Report.md)
- [F5 – In-Memory Provider and Contract Suite and Gate G5](reports/18-DDD.BuildingBlocks-F5-In-Memory-Provider-and-Contract-Suite-Report.md)
- [F7 – PostgreSQL Production Provider and Gate G7](reports/19-DDD.BuildingBlocks-F7-PostgreSQL-Production-Provider-Report.md)
- [F7.4 – SQL Server Parity Provider](reports/20-DDD.BuildingBlocks-F7.4-SQL-Server-Parity-Provider-Report.md)
- [F8 – Projections and Recovery](reports/21-DDD.BuildingBlocks-F8-Projections-and-Recovery-Report.md)
- [F9 – Versioned Snapshots](reports/22-DDD.BuildingBlocks-F9-Versioned-Snapshots-Report.md)
- [F10 – Packaging and Stable Release](reports/23-DDD.BuildingBlocks-F10-Packaging-and-Stable-Release-Report.md)

New phase reports belong under `reports/`. Numbered analysis, decision, and planning documents remain in this directory so their intended reading order stays visible.
