using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Tests.Abstracts.Event
{
    using Core.Attribute;

    [DomainEventType]
	public class OrderItemAddedToOrderEvent(string serializedAggregateId, long targetVersion, string orderItemId)
        : DomainEvent(serializedAggregateId, targetVersion, _currentTypeVersion)
    {
		public string OrderItemId
		{
			get;
		} = orderItemId;

        private static int _currentTypeVersion = 1;
    }
}
