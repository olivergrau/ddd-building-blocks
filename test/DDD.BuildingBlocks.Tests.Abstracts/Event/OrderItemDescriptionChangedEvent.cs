using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Tests.Abstracts.Event
{
    using Core.Attribute;

    [DomainEventType]
	public class OrderItemDescriptionChangedEvent(string serializedAggregateId, int version, string description)
        : DomainEvent(serializedAggregateId, version, _currentTypeVersion)
    {
		private static int _currentTypeVersion = 1;

        public string Description
		{
			get;
		} = description;
    }
}
