// Domain/Services/IIntegrationEventHandler.cs

using DDD.BuildingBlocks.Core.Event;

namespace LunarOps.Domain.Service
{
    /// <summary>
    /// Listens for the external MissionArrivedAtLunarOrbit event
    /// and creates/loads a LunarMission.
    /// </summary>
    public interface IIntegrationEventHandler
    {
        Task HandleAsync(DomainEvent @event, CancellationToken ct = default);
    }
}