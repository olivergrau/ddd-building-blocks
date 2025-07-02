namespace DDD.BuildingBlocks.Core.Exception
{
    public class AggregateNotFoundException: System.Exception
    {
        public AggregateNotFoundException(object aggregateId) : base($"Aggregate not found: {aggregateId}")
        {            
        }

        public AggregateNotFoundException(string message) : base(message)
        {            
        }
    }
}
