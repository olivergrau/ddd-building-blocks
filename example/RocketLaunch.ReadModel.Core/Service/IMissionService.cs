using RocketLaunch.ReadModel.Core.Model;

namespace RocketLaunch.ReadModel.Core.Service;

public interface IMissionService
{
    Mission? GetById(Guid missionId);
    IEnumerable<Mission> GetAll();
    Task CreateOrUpdateAsync(Mission mission);
}
