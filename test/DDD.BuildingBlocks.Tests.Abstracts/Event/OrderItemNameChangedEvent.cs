using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Tests.Abstracts.Event
{
    using Core.Attribute;

    [DomainEventType]
	public class OrderItemNameChangedEvent(string serializedAggregateId, long targetVersion, string name)
        : DomainEvent(serializedAggregateId, targetVersion, _currentTypeVersion)
    {
		private static int _currentTypeVersion = 1;

        public string Name
		{
			get;
		} = name;
    }
}
