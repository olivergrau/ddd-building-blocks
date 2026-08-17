namespace DDD.BuildingBlocks.Core.Projection;

public sealed record ProjectionRunResult(
    int ReadCount,
    int AppliedCount,
    long LastProcessedPosition,
    bool HasMore);
