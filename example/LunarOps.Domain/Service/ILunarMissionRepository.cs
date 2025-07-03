// Domain/Repositories/ILunarMissionRepository.cs

using LunarOps.Domain.Model;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.Domain.Service
{
    public interface ILunarMissionRepository
    {
        Task<LunarMission?> GetByIdAsync(ExternalMissionId id, CancellationToken ct = default);
        Task SaveAsync(LunarMission mission, CancellationToken ct = default);
    }
}