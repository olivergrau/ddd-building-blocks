using System;

namespace DDD.BuildingBlocks.Core.Event;

public sealed record EventEnvelopeMetadata(
    Guid EventId,
    string StreamId,
    string AggregateType,
    long StreamVersion,
    long? GlobalPosition,
    DateTimeOffset OccurredAt,
    DateTimeOffset? CommittedAt = null,
    string? CorrelationId = null,
    string? CausationId = null,
    string? CommandId = null,
    string? Actor = null,
    string? TurnId = null);
