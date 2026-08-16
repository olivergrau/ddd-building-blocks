# Specifications and Modernization Documentation

This folder separates governing documents from evidence produced during implementation.

## Current Architectural Baseline

- DDD.BuildingBlocks remains focused on event-sourced domain aggregates.
- The existing `AggregateRoot<TKey>` is renamed to `EventSourcedAggregateRoot<TKey>` in F2 as an intentional breaking change.
- No `ConventionalAggregateRoot<TKey>`, `PlainAggregateRoot<TKey>`, or marker-only `IAggregateRoot<TKey>` is introduced.
- Relational persistence remains valid for projections and non-aggregate technical or operational state.

## Analysis and Planning

1. [08A – Framework Analysis](08A-DDD.BuildingBlocks-Framework-Analysis.md)
2. [08B – Modernization Plan](08B-DDD.BuildingBlocks-Modernization-Plan.md)
3. [08C – Usage and Release Gate](08C-DDD.BuildingBlocks-Usage-and-Release-Gate.md)
4. [10 – Detailed Implementation Plan](10-DDD.BuildingBlocks-Detailed-Implementation-Plan.md)
5. [11 – Linux, VS Code, and Dev Container Development Environment](11-Linux-VS-Code-and-Devcontainer-Development-Environment.md)
6. [12 – Two-Repository and Packaging Strategy](12-Two-Repository-and-Packaging-Strategy.md)

## Preparatory Working Document

- [Preparation Plan Before Implementation](Preparation-Plan-Before-Implementation.md)

## Implementation Reports and Gate Evidence

- [F0 – Baseline Report](reports/13-DDD.BuildingBlocks-F0-Baseline-Report.md)
- [F1 – Outcome Report and Gate G1](reports/14-DDD.BuildingBlocks-F1-Outcome-Report.md)

New phase reports belong under `reports/`. Numbered analysis, decision, and planning documents remain in this directory so their intended reading order stays visible.
