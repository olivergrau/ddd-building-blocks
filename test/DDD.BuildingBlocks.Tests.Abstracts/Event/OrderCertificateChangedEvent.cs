using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Tests.Abstracts.Event
{
    using Core.Attribute;

    [DomainEventType]
	public class OrderCertificateChangedEvent(string serializedAggregateId, int version, string prefix, string code)
        : DomainEvent(serializedAggregateId, version, _currentTypeVersion)
    {
		private static int _currentTypeVersion = 1;

        public string Prefix
		{
			get;
		} = prefix;

        public string Code
		{
			get;
		} = code;
    }
}
