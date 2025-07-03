// Domain/Services/IIntegrationEventPublisher.cs

using DDD.BuildingBlocks.Core.Event;

namespace RocketLaunch.Domain.Service
{
    public interface IIntegrationEventPublisher
    {
        Task PublishAsync(DomainEvent @event, CancellationToken ct = default);
    }
}