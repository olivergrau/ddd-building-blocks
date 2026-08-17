using System;

namespace DDD.BuildingBlocks.Core.Projection;

public sealed record ProjectionCheckpoint(
    ProjectionKey Key,
    long LastProcessedPosition,
    ProjectionStatus Status,
    long? FailedPosition,
    string? LastError,
    DateTimeOffset UpdatedAt);
