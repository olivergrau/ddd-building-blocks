using System.Collections.Concurrent;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;

namespace RocketLaunch.ReadModel.InMemory.Service;

public class InMemoryMissionService : IMissionService
{
    private readonly ConcurrentDictionary<Guid, Mission> _missions = new();

    public Mission? GetById(Guid missionId)
    {
        _missions.TryGetValue(missionId, out var mission);
        return mission;
    }

    public IEnumerable<Mission> GetAll()
    {
        return _missions.Values;
    }

    public Task CreateOrUpdateAsync(Mission mission)
    {
        _missions[mission.MissionId] = mission;
        return Task.CompletedTask;
    }
}
