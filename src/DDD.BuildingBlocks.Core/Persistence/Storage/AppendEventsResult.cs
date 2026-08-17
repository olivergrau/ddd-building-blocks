namespace DDD.BuildingBlocks.Core.Persistence.Storage;

public sealed record AppendEventsResult(
    long CurrentStreamVersion,
    long FirstGlobalPosition,
    long LastGlobalPosition);
