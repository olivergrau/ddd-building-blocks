using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Tests.Abstracts.Event
{
    using Core.Attribute;

    [DomainEventType]
	public class OrderItemNameChangedEvent(string serializedAggregateId, int version, string name)
        : DomainEvent(serializedAggregateId, version, _currentTypeVersion)
    {
		private static int _currentTypeVersion = 1;

        public string Name
		{
			get;
		} = name;
    }
}
