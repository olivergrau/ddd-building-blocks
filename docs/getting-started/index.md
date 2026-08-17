# Getting started

This section takes you from package download to a persisted and rehydrated aggregate.

## Learning path

1. [Prerequisites and installation](installation.md)
2. [Quickstart: your first event-sourced aggregate](quickstart.md)
3. [Where to go next](next-steps.md)

The quickstart uses the in-memory provider so that the event-sourcing mechanics are visible without database setup. The same repository and codec contracts are used with PostgreSQL and SQL Server later.

## What you should know

Familiarity with C#, dependency injection, and basic DDD terminology is useful. You do not need prior experience implementing an event store. The [concept articles](../concepts/index.md) explain the framework-specific model.

## What the quickstart proves

At the end you will have:

- a strongly typed aggregate identifier;
- a versioned domain event with a stable storage key;
- an aggregate that changes state only by raising and applying events;
- an event codec and in-memory event store;
- an `EventSourcingRepository` that saves and rehydrates the aggregate;
- an understanding of committed and uncommitted stream versions.
