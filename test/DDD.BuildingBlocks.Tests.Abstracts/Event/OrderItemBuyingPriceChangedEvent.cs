using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Tests.Abstracts.Event
{
    using Core.Attribute;

    [DomainEventType]
	public class OrderItemBuyingPriceChangedEvent(string serializedAggregateId, long targetVersion, decimal buyingPrice)
        : DomainEvent(serializedAggregateId, targetVersion, _currentTypeVersion)
    {
		private static int _currentTypeVersion = 1;

        public decimal BuyingPrice
		{
			get;
		} = buyingPrice;
    }
}
