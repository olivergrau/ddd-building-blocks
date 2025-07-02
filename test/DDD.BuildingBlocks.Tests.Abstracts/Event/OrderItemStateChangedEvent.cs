using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Tests.Abstracts.Event
{
    using Core.Attribute;

    [DomainEventType]
	public class OrderItemStateChangedEvent(string serializedAggregateId, int version, int state)
        : DomainEvent(serializedAggregateId, version, _currentTypeVersion)
    {
		public int State
		{
			get;
		} = state;

        private static int _currentTypeVersion = 1;
    }
}
