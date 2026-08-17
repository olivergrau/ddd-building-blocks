# F5 Outcome Report: In-Memory Provider and Contract Suite

## Status

F5 is implemented, verified, and accepted at Gate G5.

## Contract boundary

The legacy `IEventStorageProvider` stores mutable domain-event objects and cannot express atomic expected-version append, provider-assigned global positions, unique event IDs, or a committed feed. F5 therefore introduces `IEventStoreProvider` as the normative provider contract. It persists immutable `EventEnvelope` records and exposes:

- atomic batch append with an exact expected stream version;
- ordered stream reads from an inclusive stream version;
- committed-feed reads after an exclusive global position.

The migration was completed inside F5:

- `IEventStorageProvider` was removed;
- `EventSourcingRepository` now encodes and decodes envelopes through `IEventCodec` and persists only through `IEventStoreProvider`;
- RocketLaunch and the repository suites use explicit stable event registrations and the new in-memory provider;
- `PureInMemoryEventStorageProvider`, `FileInMemoryEventStorageProvider`, and their file-mapping support were removed;
- the CLR-type-bound legacy MSSQL event, snapshot, feed-worker, and aggregate-mapping implementations were removed;
- the MSSQL project remains an unpackaged placeholder for a future clean-room provider after PostgreSQL is accepted.

The legacy provider-side `UniqueDomainPropertyAttribute` behavior was also removed. Cross-stream domain uniqueness is not an event-store invariant and must be modeled through an explicit domain reservation, process, or appropriately transactional supporting store rather than reflection over aggregate state inside a generic event provider.

## In-memory reference provider

`InMemoryEventStoreProvider` provides the reference semantics:

- one asynchronous synchronization boundary protects validation and commit;
- expected version is checked inside the atomic append boundary;
- all envelope stream versions in a batch must be contiguous;
- event IDs are unique both within a batch and across the store;
- rejected batches do not mutate streams, the event-ID index, or the committed feed;
- global positions are provider-assigned, monotone, and visible only after commit;
- stream and feed order are stable;
- commit timestamps are provider-assigned;
- stored payloads remain valid and immutable independently of their source JSON document;
- cancellation is honored without wrapping `OperationCanceledException`;
- missing streams, existing streams, version conflicts, and duplicate event IDs have explicit classified errors.

The provider is intentionally process-local and volatile. It defines semantics and supports tests; it is not presented as durable production storage.

The RocketLaunch projection example uses `PublishingEventStoreProviderDecorator` around the provider. The decorator publishes only after a successful atomic append, keeping publication outside the normative storage contract and preventing failed writes from becoming visible to projections.

## Normative provider-contract suite

`EventStoreProviderContract` is an abstract reusable suite. A provider supplies only a factory and must pass the same tests for:

- new-stream creation and multi-event append;
- stream identity as the combination of aggregate type and stream ID;
- stream reads from a version with a count limit;
- atomic rejection of an incorrect expected version;
- parallel writers using the same expected version;
- missing-stream and existing-stream classification;
- duplicate Event ID rejection without partial commit;
- monotone global positions and exclusive feed paging;
- immutable stored payloads;
- unwrapped cancellation.

Snapshot activation remains optional in `EventSourcingRepository`, and full replay without snapshots remains mandatory. Snapshot persistence stays a separate provider concern; F9 must deliver its normative contract and implementations for In-Memory, PostgreSQL, and SQL Server. Snapshot persistence failure must not alter event-commit success.

Projection-checkpoint contracts remain deferred until the projection phase.

## Verification

- Solution build: successful with zero warnings and zero errors.
- Package build: the five active packages and their symbol packages were produced successfully. The empty MSSQL placeholder is deliberately not packable.
- Core: 64/64 passed.
- DevelopmentPackage integration: 28/28 passed, including all F5 provider contracts and repository rehydration through the codec.
- The legacy MSSQL integration suite was removed with the non-conforming provider. A future MSSQL provider must inherit the normative suite.
- RocketLaunch Application: 23/23 passed; ReadModel: 27/27 passed.
- RocketLaunch Domain: 21/21 passed; LunarOps Domain: 53/53 passed.
- RocketLaunch API: 5/7 passed; both failures are manifestations of the previously accepted asynchronous projection timing race.

## Gate G5 review

Gate G5 accepts `IEventStoreProvider`, its exact version and paging semantics, its classified failure behavior, and `EventStoreProviderContract` as normative for every future event-store provider. Do not begin F6 before explicit approval.
