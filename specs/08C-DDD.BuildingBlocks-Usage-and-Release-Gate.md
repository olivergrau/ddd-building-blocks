# Persona Simulation Playground

## Work package 8C: DDD.BuildingBlocks deployment and release gate

**Status:** REVIEW DRAFT  
**Version:** 1.1  
**Date:** 2026-08-16

## 1. Purpose

This document complements the framework analysis `08A` and the modernization plan `08B`. It defines when DDD.BuildingBlocks will be released as a reliable basis for the playground and which decisions will consciously remain open until then.

## 2. Usage decision

DDD.BuildingBlocks is used as a tactical DDD foundation and is specifically modernized before Playground implementation begins.

In particular, the following are taken over:

- `Entity`, `EntityId`, `ValueObject`, and `AggregateRoot`;
- event-sourced domain aggregates with explicit stream semantics;
- Command and event processing;
- Provider interfaces;
- Event Store, snapshot and projection concepts;
- Testing support.

No competing second building blocks or event sourcing base will be established in the Playground.

## 3. Order

```text
F0 reproduce the baseline
-> F1 through F5 modernize and harden the core
-> F6 select the production provider
-> F7 implement the production provider
-> F8 demonstrate projection and recovery
-> optional F9 snapshot hardening
-> F10 release and explicit approval
-> only then begin the Playground foundation
```

## 4. Release criteria

### Build and platform

- .NET 10 build reproducible on Linux;
- no unexplained compiler warnings;
- Nullable strategy consistent;
- Package dependencies current and justified;
- Examples are built or are clearly marked as historical.

### Tactical model

- event-sourcing semantics are explicit in `AggregateRoot<TKey>`;
- no conventional or plain aggregate-root abstraction dilutes the framework contract;
- the retained `AggregateRoot<TKey>` name is documented as the single event-sourced root model;
- uncommitted events and replay are encapsulated;
- Apply is deterministic;
- Version semantics are documented;
- tactical types do not enforce infrastructure dependency in the Playground domain model.

### Event Contracts

- stable event type keys instead of persisted CLR names;
- Schema versioning and registry;
- Event Envelope separates technical metadata from domain payload;
- Upcasting path tested at least as an example;
- Correlation and causation are possible.

### Providers

- provider-neutral contracts are defined;
- In-memory provider is thread-safe and close to production in its semantics;
- exactly one production provider is selected for the initial use case;
- both consist of the same core contract suite;
- The production provider passes additional integration, concurrency, and recovery tests against a real store.

### Projections and Recovery

- committed event feed;
- Checkpoint atomically or otherwise correctly coupled;
- idempotent processing;
- Retry and visible error status;
- complete rebuild;
- Projection Failure does not change a successful domain commit.

### API quality

- Async and cancellation at I/O limits;
- no service locator dependency in normal runtime path;
- Conflict, validation and infrastructure failure can be distinguished;
- DI registration explicit and testable.

### Evidence

- Unit testing;
- Provider contract testing;
- production-provider integration tests against a real store;
- Migration and recovery;
- Example or reference application for the intended Happy Path;
- short upgrade and usage documentation.

## 5. Production Provider Decision

The technology is not anticipated in the specification phase. At least the following are checked at gate F6:

- required event store semantics;
- atomic expected version check;
- committed feed and projection connection;
- Migration, backup and restore;
- Operation in Homelab and k3s;
- Test containers support;
- Maintenance and upgrade effort;
- additional infrastructure requirements.

PostgreSQL is the preferred starting hypothesis because it is already selected for read models and other relational operational data. An external event store is preferable only if it provides a demonstrable functional or operational benefit that justifies the additional complexity.

## 6. Release artifact

The release takes place via a clearly versioned framework version. The playground does not reference a random local working copy or unmarked branch.

The release contains:

- version number or tag;
- release notes;
- known limitations;
- Migration notices;
- tested .NET and provider versions;
- Reference to green test runs.

## 7. Stop and escalation rules

Blocking are:

- unreproducible baseline build;
- unclear or inconsistent aggregate versioning;
- no atomic concurrency check in the selected production provider;
- persisted dependency on CLR type names without migration path;
- non-rebuildable projections;
- provider contracts that allow in-memory and production providers to diverge semantically;
- Framework API that forces infrastructure into the Playground domain model.

Non-blocking, but to be documented:

- cosmetic API names;
- additional convenience APIs;
- adaptive snapshot policy;
- other providers;
- Optimizations without measured need.

## 8. Formal release

After F10, a short report is submitted with:

```text
commit and release used
criteria met
test evidence
known limitations
selected production provider
open future extensions
```

The playground only begins after Oliver's explicit decision:

```text
READY FOR PLAYGROUND
```

## 9. Acceptance criteria for work package 8

- Analysis, modernization plan and release gate form a complete process.
- Framework modernization comes before playground code.
- The production-provider technology remains open until F6.
- PostgreSQL is a preference, but not an unfounded pre-determination of the event store.
- Real integration and contract tests are mandatory.
- there is exactly one released tactical basis.
