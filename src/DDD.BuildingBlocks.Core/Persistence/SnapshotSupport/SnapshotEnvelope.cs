using System;
using System.Text.Json;

namespace DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;

public sealed record SnapshotEnvelope
{
    public SnapshotEnvelope(
        string streamId,
        string aggregateType,
        long streamVersion,
        string snapshotType,
        int schemaVersion,
        DateTimeOffset createdAt,
        JsonElement payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentOutOfRangeException.ThrowIfNegative(streamVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotType);
        ArgumentOutOfRangeException.ThrowIfLessThan(schemaVersion, 1);
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Snapshot payload must be a JSON object.", nameof(payload));
        }

        StreamId = streamId;
        AggregateType = aggregateType;
        StreamVersion = streamVersion;
        SnapshotType = snapshotType;
        SchemaVersion = schemaVersion;
        CreatedAt = createdAt;
        Payload = payload.Clone();
    }

    public string StreamId { get; }
    public string AggregateType { get; }
    public long StreamVersion { get; }
    public string SnapshotType { get; }
    public int SchemaVersion { get; }
    public DateTimeOffset CreatedAt { get; }
    public JsonElement Payload { get; }
}
