using RocketLaunch.ReadModel.Core.Model;

namespace RocketLaunch.ReadModel.Core.Service;

public interface IRocketService
{
    Task<Rocket?> GetByIdAsync(Guid rocketId);
    Task<IEnumerable<Rocket>> GetAllAsync();

    /// <summary>
    /// Returns true if the rocket is available (e.g., not assigned, not in maintenance)
    /// </summary>
    Task<bool> IsAvailableAsync(Guid rocketId);

    /// <summary>
    /// Returns all available rockets with at least the given payload and crew capacity
    /// </summary>
    Task<IEnumerable<Rocket>> FindAvailableAsync(int minPayloadKg, int minCrewCapacity);
    
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
    Task<Rocket?> FindByAssignedMissionAsync(Guid missionId);


}
