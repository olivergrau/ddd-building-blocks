using System;

namespace DDD.BuildingBlocks.Core.Exception
{
    [Serializable]
    public class NotFoundException(object? aggregateId, string? message = null, System.Exception? inner = null)
        : AggregateException(aggregateId, message, inner);
}