using System;

namespace DDD.BuildingBlocks.Core.Exception
{
    [Serializable]
    public class InvalidStateException(object? aggregateId, string? message, System.Exception? inner = null) : AggregateException(aggregateId, message, inner);
}