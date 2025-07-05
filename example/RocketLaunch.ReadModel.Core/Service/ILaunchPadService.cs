using RocketLaunch.ReadModel.Core.Model;

namespace RocketLaunch.ReadModel.Core.Service;

public interface ILaunchPadService
{
    LaunchPad? GetById(Guid padId);

    /// <summary>
    /// Returns true if the launch pad is available for the given time window
    /// </summary>
    bool IsAvailable(Guid padId, DateTime windowStart, DateTime windowEnd);

    /// <summary>
    /// Finds all launch pads that support the given rocket type and are free for the time window
    /// </summary>
    IEnumerable<LaunchPad> FindAvailable(string rocketType, DateTime windowStart, DateTime windowEnd);
    
    /// <summary>
    /// Find a launch pad that is currently assigned to a specific mission
    /// </summary>
    /// <param name="missionId"></param>
    /// <returns></returns>
    LaunchPad? FindByAssignedMission(Guid missionId);
    
    /// <summary>
    /// Updates the launch pad information in the read model
    /// </summary>
    /// <param name="pad"></param>
    /// <returns></returns>
    Task CreateOrUpdateAsync(LaunchPad pad);
}
