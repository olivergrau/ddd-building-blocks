using System;
using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Tests.Abstracts.Event
{
    using Core.Attribute;

    [DomainEventType]
	public class OrderCreatedEvent(
        string serializedAggregateId,
        int version,
        string title,
        string comment,
        int state,
        DateTime createdTime
    ) : DomainEvent(serializedAggregateId, version, _currentTypeVersion)
    {
		private static int _currentTypeVersion = 1;

		public string Title
		{
			get;
		} = title;

        public string Comment
		{
			get;
		} = comment;

        public DateTime CreatedTime
		{
			get;
		} = createdTime;

        public int State
		{
			get;
		} = state;
    }
}
