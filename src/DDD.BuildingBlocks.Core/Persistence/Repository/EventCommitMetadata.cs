namespace DDD.BuildingBlocks.Core.Persistence.Repository;

public sealed record EventCommitMetadata(
    string? CorrelationId = null,
    string? CausationId = null,
    string? CommandId = null,
    string? Actor = null,
    string? TurnId = null);
