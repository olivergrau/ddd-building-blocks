
namespace DDD.BuildingBlocks.Core.Exception
{
    public class AggregateValidationException(object aggregateId, string propertyName, object value, string optionalHint = "")
        : System.Exception($"Aggregate [{aggregateId}]: property [{propertyName} = {value}] validation error: {optionalHint}")
    {
        public object AggregateId { get; } = aggregateId;
    }
}