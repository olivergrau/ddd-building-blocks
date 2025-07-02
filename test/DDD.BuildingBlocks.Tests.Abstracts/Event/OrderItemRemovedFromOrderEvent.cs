using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Tests.Abstracts.Event
{
    using Core.Attribute;

    [DomainEventType]
	public class OrderItemRemovedFromOrderEvent(string orderId, int version, string orderItemId) : DomainEvent(orderId, version, _currentTypeVersion)
    {
		public string OrderItemId
		{
			get;
		} = orderItemId;

        private static int _currentTypeVersion = 1;
    }
}
