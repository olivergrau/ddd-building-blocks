using RocketLaunch.ReadModel.Core.Model;

namespace RocketLaunch.ReadModel.Core.Service;

public interface IRocketService
{
    Rocket? GetById(Guid rocketId);
    IEnumerable<Rocket> GetAll();

    /// <summary>
    /// Returns true if the rocket is available (e.g., not assigned, not in maintenance)
    /// </summary>
    bool IsAvailable(Guid rocketId);

    /// <summary>
    /// Returns all available rockets with at least the given payload and crew capacity
    /// </summary>
    IEnumerable<Rocket> FindAvailable(int minPayloadKg, int minCrewCapacity);
    
    /// <summary>
    /// Saves a new rocket to the read model
    /// </summary>
    /// <param name="rocket"></param>
    /// <returns></returns>
    Task CreateOrUpdateAsync(Rocket rocket);
    
    /// <summary>
    /// Find a rocket that is currently assigned to a specific mission
    /// </summary>
    /// <param name="missionId"></param>
    /// <returns></returns>
    Rocket? FindByAssignedMission(Guid missionId);


}
