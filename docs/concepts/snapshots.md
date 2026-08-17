# Snapshots

## Purpose

A snapshot is a cached aggregate state at a known stream version. It reduces the number of events needed for rehydration of long streams.

Snapshots are not authoritative history. The event stream must remain sufficient for full replay.

## Snapshot envelope

`SnapshotEnvelope` contains:

- stream ID and aggregate type;
- stream version represented by the snapshot;
- stable snapshot type key;
- positive schema version;
- creation timestamp;
- immutable JSON payload.

`SnapshotTypeRegistry` maps stable keys to snapshot CLR types. `SystemTextJsonSnapshotCodec` encodes and decodes registered snapshot classes.

## Repository behavior

Snapshot support is enabled only when both `ISnapshotStoreProvider` and `ISnapshotCodec` are supplied. Partial configuration is rejected.

On load, the repository:

1. asks for the latest snapshot not newer than the requested stream version;
2. decodes it;
3. applies it to the aggregate;
4. replays residual events after the snapshot version.

If the key is unknown, the schema version is incompatible, the payload is malformed, or identity/version checks fail, the codec returns no snapshot and the repository performs full replay.

## Commit isolation

Events commit first. The aggregate is marked committed immediately after a successful append. Snapshot creation and persistence happen afterward and cannot change event-commit success.

This means a snapshot provider outage may increase future rehydration time but must not make callers retry an event batch that already committed.

## Frequency

Each provider exposes `SnapshotFrequency`. The repository takes a snapshot when the aggregate crosses the configured boundary, including large event batches that cross more than one boundary.

Choose frequency from measured replay cost, event volume, snapshot size, and write load. A lower number is not automatically better.

## Evolution strategy

Snapshot evolution is intentionally conservative. Unlike events, the default snapshot codec has no upcaster chain. When a snapshot schema changes, register the new current version and allow old snapshots to be discarded. They will be rebuilt from events.

This is why event evolution must be lossless while snapshot compatibility may be disposable.
