using System;

namespace DDD.BuildingBlocks.Core.Exception
{
    [Serializable]
    public class AggregateVersionConflictException(
        object? aggregateId,
        string? message,
        long expectedVersion,
        long targetVersion,
        System.Exception? inner = null
    ) : AggregateException(aggregateId, message, inner)
    {
        public long ExpectedVersion { get; } = expectedVersion;
        public long TargetVersion { get; } = targetVersion;
    }
}