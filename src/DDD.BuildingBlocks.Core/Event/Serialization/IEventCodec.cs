namespace DDD.BuildingBlocks.Core.Event.Serialization;

public interface IEventCodec
{
    EventEnvelope Encode(IDomainEvent domainEvent, EventEnvelopeMetadata metadata);
    IDomainEvent Decode(EventEnvelope envelope);
}
