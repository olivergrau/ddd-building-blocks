// Domain/Repositories/IMissionRepository.cs

using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Domain.Model;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Domain.Service
{
    public interface IMissionRepository : IEventSourcingRepository
    {
        Task<Mission?> GetByIdAsync(MissionId id, CancellationToken ct = default);
        Task SaveAsync(Mission mission, CancellationToken ct = default);
    }
}