namespace DDD.BuildingBlocks.Core.Message
{
    public interface IMessage
    {
        string? CorrelationId { get; }
    }
}
