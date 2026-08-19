namespace DDD.BuildingBlocks.Core.Exception;

public sealed class UnknownAggregateTypeException(string aggregateType)
    : System.Exception($"Aggregate type '{aggregateType}' is not registered.");
