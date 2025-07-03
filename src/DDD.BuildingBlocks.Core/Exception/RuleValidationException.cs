namespace DDD.BuildingBlocks.Core.Exception;

public class RuleValidationException(object aggregateId, string rule, string optionalHint = "")
    : System.Exception($"Aggregate [{aggregateId}]: rule [{rule} = {rule}] validation error: {optionalHint}")
{
    public object AggregateId { get; } = aggregateId;
}