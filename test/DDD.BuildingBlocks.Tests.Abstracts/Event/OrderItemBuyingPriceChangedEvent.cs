using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Tests.Abstracts.Event
{
    using Core.Attribute;

    [DomainEventType]
	public class OrderItemBuyingPriceChangedEvent(string serializedAggregateId, int version, decimal buyingPrice)
        : DomainEvent(serializedAggregateId, version, _currentTypeVersion)
    {
		private static int _currentTypeVersion = 1;

        public decimal BuyingPrice
		{
			get;
		} = buyingPrice;
    }
}
