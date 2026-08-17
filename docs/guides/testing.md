# Testing strategy

Event-sourced systems need tests at several boundaries. A passing aggregate unit test does not prove historical serialization, provider concurrency, or recoverable projection behavior.

## Aggregate behavior tests

Test business decisions with given/when/then structure:

```text
Given: historical events or an aggregate in a known state
When:  one business method is invoked
Then:  expected new event(s), rejection, and resulting state
```

Cover:

- valid lifecycle transitions;
- invariant rejection;
- boundary values in value objects;
- wrong stream identity and target version;
- deactivated/closed aggregate behavior;
- every internal event handler.

Keep external availability ports deterministic with stubs. Do not mock aggregate internals.

## Event-codec tests

For every registered event:

- encode and decode a representative current event;
- assert stable key and schema version;
- assert envelope metadata survives appropriately;
- keep JSON fixtures for every historical schema;
- run the complete upcaster chain to the current CLR type;
- assert unknown keys, future schemas, missing steps, and malformed JSON fail explicitly.

Fixtures should be checked into the consumer repository. Never regenerate old fixtures from the current event class during the test.

## Repository integration tests

Save an aggregate, create a new repository instance, and reload it. Assert domain-observable state, `CurrentVersion`, and `LastCommittedVersion`.

Also test:

- multiple events in one atomic save;
- loading a missing stream;
- concurrent writers from the same committed version;
- cancellation before and during provider calls;
- failed append retaining uncommitted changes;
- correlation metadata where used.

## Provider contract tests

The framework repository includes reusable provider-contract suites. A custom provider should run the same semantics unchanged rather than inventing provider-specific expectations.

Use a real database instance for relational providers. Testcontainers is appropriate for automated integration tests because it exercises locks, transactions, constraints, JSON types, and actual driver behavior.

## Snapshot tests

Prove:

- full replay equals snapshot plus residual replay;
- latest and bounded snapshot reads;
- aggregate-type isolation for equal stream IDs;
- incompatible/corrupt snapshots fall back to replay;
- snapshot write failure does not alter event-commit success;
- snapshot frequency does not change final state.

## Projection tests

Cover:

- initial checkpoint;
- read-model update and checkpoint in one transaction;
- idempotent redelivery;
- rollback on handler failure;
- visible failed position and diagnostic;
- explicit retry without skipping;
- independent progress for two projections;
- reset and full rebuild equality.

Relational projection tests must write through the transaction context, then deliberately throw to prove both row and checkpoint roll back.

## Operational tests

Before production, automate:

- large append batches within supported limits;
- concurrent append stress;
- forced connection/session failure and rollback;
- migration idempotency;
- native backup and restore;
- projection rebuild from a restored database;
- clean consumer restore from the produced NuGet packages.

## Test isolation

Use unique schemas or databases per test collection. Do not run destructive reset/rebuild tests against a shared developer database. Dispose data sources and containers predictably.
