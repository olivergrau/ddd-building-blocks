using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Tests.Abstracts.Event
{
    using Core.Attribute;

    [DomainEventType]
	public class OrderTitleChangedEvent(string serializedAggregateId, int version, string title)
        : DomainEvent(serializedAggregateId, version, _currentTypeVersion)
    {
		private static int _currentTypeVersion = 1;

        public string Title
		{
			get;
		} = title;
    }
}
