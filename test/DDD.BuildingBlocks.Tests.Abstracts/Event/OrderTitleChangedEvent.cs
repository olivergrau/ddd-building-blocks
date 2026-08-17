using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Tests.Abstracts.Event
{
    using Core.Attribute;

    [DomainEventType]
	public class OrderTitleChangedEvent(string serializedAggregateId, long targetVersion, string title)
        : DomainEvent(serializedAggregateId, targetVersion, _currentTypeVersion)
    {
		private static int _currentTypeVersion = 1;

        public string Title
		{
			get;
		} = title;
    }
}
