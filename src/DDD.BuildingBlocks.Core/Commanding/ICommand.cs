namespace DDD.BuildingBlocks.Core.Commanding;

using Message;

public interface ICommand : IMessage
{
    AggregateSourcingMode Mode { get; set; }

    string? SerializedAggregateId { get; }

    int TargetVersion { get; }
}
