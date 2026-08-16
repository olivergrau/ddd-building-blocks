using System;
using System.Text.Json;

namespace DDD.BuildingBlocks.Core.Event;

public sealed record EventEnvelope
{
    public EventEnvelope(
        Guid eventId,
        string streamId,
        string aggregateType,
        long streamVersion,
        long? globalPosition,
        string eventType,
        int schemaVersion,
        DateTimeOffset occurredAt,
        DateTimeOffset? committedAt,
        string? correlationId,
        string? causationId,
        string? commandId,
        string? actor,
        string? turnId,
        JsonElement payload)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event ID must not be empty.", nameof(eventId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentOutOfRangeException.ThrowIfLessThan(streamVersion, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(schemaVersion, 1);

        if (globalPosition is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(globalPosition), globalPosition, "Global position must not be negative.");
        }

        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Event payload must be a JSON object.", nameof(payload));
        }

        EventId = eventId;
        StreamId = streamId;
        AggregateType = aggregateType;
        StreamVersion = streamVersion;
        GlobalPosition = globalPosition;
        EventType = eventType;
        SchemaVersion = schemaVersion;
        OccurredAt = occurredAt;
        CommittedAt = committedAt;
        CorrelationId = correlationId;
        CausationId = causationId;
        CommandId = commandId;
        Actor = actor;
        TurnId = turnId;
        Payload = payload.Clone();
    }

    public Guid EventId { get; }
    public string StreamId { get; }
    public string AggregateType { get; }
    public long StreamVersion { get; }
    public long? GlobalPosition { get; }
    public string EventType { get; }
    public int SchemaVersion { get; }
    public DateTimeOffset OccurredAt { get; }
    public DateTimeOffset? CommittedAt { get; }
    public string? CorrelationId { get; }
    public string? CausationId { get; }
    public string? CommandId { get; }
    public string? Actor { get; }
    public string? TurnId { get; }
    public JsonElement Payload { get; }
}
