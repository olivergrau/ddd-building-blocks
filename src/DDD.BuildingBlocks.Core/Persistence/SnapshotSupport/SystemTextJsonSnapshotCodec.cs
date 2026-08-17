using System;
using System.Text.Json;

namespace DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;

public sealed class SystemTextJsonSnapshotCodec : ISnapshotCodec
{
    private readonly SnapshotTypeRegistry _types;
    private readonly JsonSerializerOptions _options;

    public SystemTextJsonSnapshotCodec(SnapshotTypeRegistry types, JsonSerializerOptions? options = null)
    {
        _types = types ?? throw new ArgumentNullException(nameof(types));
        _options = options is null
            ? new JsonSerializerOptions(JsonSerializerDefaults.Web)
            : new JsonSerializerOptions(options);
    }

    public SnapshotEnvelope Encode(Snapshot snapshot, string aggregateType)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        var registration = _types.GetByType(snapshot.GetType());
        var payload = JsonSerializer.SerializeToElement(snapshot, snapshot.GetType(), _options);
        return new SnapshotEnvelope(
            snapshot.SerializedAggregateId,
            aggregateType,
            snapshot.Version,
            registration.SnapshotType,
            registration.CurrentSchemaVersion,
            DateTimeOffset.UtcNow,
            payload);
    }

    public Snapshot? Decode(SnapshotEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var registration = _types.FindByKey(envelope.SnapshotType);
        if (registration is null || envelope.SchemaVersion != registration.CurrentSchemaVersion)
        {
            return null;
        }

        try
        {
            var snapshot = JsonSerializer.Deserialize(envelope.Payload, registration.ClrType, _options) as Snapshot;
            return snapshot is not null &&
                   string.Equals(snapshot.SerializedAggregateId, envelope.StreamId, StringComparison.Ordinal) &&
                   snapshot.Version == envelope.StreamVersion
                ? snapshot
                : null;
        }
        catch (System.Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException)
        {
            return null;
        }
    }
}
