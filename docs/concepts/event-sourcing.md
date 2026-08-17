# Event-sourcing lifecycle

## One stream per aggregate instance

The durable identity of a stream is the pair:

```text
(aggregate type, stream ID)
```

Including aggregate type prevents different aggregate classes with the same textual ID from sharing a stream accidentally.

## Version model

Stream versions are signed 64-bit integers (`long`).

- `-1` means no stream exists.
- the creation event targets version `-1` and is stored at version `0`;
- each later event targets the aggregate's current version;
- applying an event increments `CurrentVersion` by one;
- `LastCommittedVersion` is the expected version used for the next append.

New events must be contiguous. Providers reject gaps, duplicates, and expected-version mismatches.

## Raise, apply, and commit

Calling `RaiseEvent` performs four operations:

1. validate the event's stream identity and target version;
2. invoke the matching aggregate handler;
3. increment the aggregate version;
4. add the event to an immutable view of uncommitted changes.

`EventSourcingRepository.SaveAsync` encodes all uncommitted events and calls `IEventStoreProvider.AppendAsync` once. Only a successful atomic append causes `MarkChangesAsCommitted`.

If append fails, the aggregate retains its uncommitted events so the caller does not mistake an unpersisted change for a commit.

## Replay

Repository loading reads stream envelopes, decodes them, creates an empty aggregate instance, and calls `ReplayEvents`. Replay:

- invokes the same internal state handlers;
- does not add events to the uncommitted collection;
- restores correlation IDs from history;
- ends with `LastCommittedVersion == CurrentVersion`.

Replay is deterministic. Aggregate event handlers must not read clocks, generate random values, call services, publish messages, or perform I/O.

## Optimistic concurrency

Expected-version append detects concurrent decisions made from stale state:

```text
Writer A loads version 4
Writer B loads version 4
Writer A appends with expected version 4 -> succeeds at version 5
Writer B appends with expected version 4 -> concurrency conflict
```

Do not blindly retry the same stale event. Reload the aggregate and re-run the business decision. The new state may reject the command or produce a different event.

## Event stream versus committed feed

Stream order answers, “What happened to this aggregate?” The committed global feed answers, “What committed across all aggregates after position N?”

Repositories use stream reads for rehydration. Projections use the committed feed for recoverable read-model processing. A feed position is a cursor, not an aggregate version.

## Publication boundary

The event-store provider guarantees persistence, not message-bus delivery. External publication should use a transactional outbox or another explicit post-commit mechanism. The Development package includes a publishing decorator for local/example use, but production delivery semantics belong in application infrastructure.
