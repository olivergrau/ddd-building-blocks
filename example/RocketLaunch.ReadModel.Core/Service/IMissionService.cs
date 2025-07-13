using RocketLaunch.ReadModel.Core.Model;

namespace RocketLaunch.ReadModel.Core.Service;

public interface IMissionService
{
    Task<Mission?> GetByIdAsync(Guid missionId);
    Task<IEnumerable<Mission>> GetAllAsync();
    Task CreateOrUpdateAsync(Mission mission);
}
