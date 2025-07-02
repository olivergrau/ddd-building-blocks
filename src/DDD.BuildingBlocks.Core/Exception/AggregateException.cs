namespace DDD.BuildingBlocks.Core.Exception
{
    public class AggregateException(object? id, string? message, System.Exception? inner = null) : System.Exception(message, inner)
    {
        public object? AggregateId { get; } = id;
    }
}