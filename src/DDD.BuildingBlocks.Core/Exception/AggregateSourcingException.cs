namespace DDD.BuildingBlocks.Core.Exception
{
    public class AggregateSourcingException : AggregateException
    {
        public AggregateSourcingException(object? id, System.Exception? inner = null) : base(id,
            "Aggregate could not be sourced.", inner)
        {
        }

        public AggregateSourcingException(object? id, string? message, System.Exception? inner = null) : base(id, message,
            inner)
        {
        }
    }
}