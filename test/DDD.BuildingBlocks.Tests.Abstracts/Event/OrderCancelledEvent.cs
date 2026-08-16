using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Tests.Abstracts.Event
{
    using Core.Attribute;

    [DomainEventType]
    public class OrderCancelledEvent(string serializedAggregateId, long targetVersion) : DomainEvent(serializedAggregateId, targetVersion, _currentTypeVersion)
    {
		private static int _currentTypeVersion = 1;
    }
}
