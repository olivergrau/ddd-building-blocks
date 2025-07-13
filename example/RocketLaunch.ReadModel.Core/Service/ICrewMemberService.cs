using RocketLaunch.ReadModel.Core.Model;

namespace RocketLaunch.ReadModel.Core.Service;

public interface ICrewMemberService
{
    Task<CrewMember?> GetByIdAsync(Guid crewMemberId);
    Task<IEnumerable<CrewMember>> GetAllAsync();

    /// <summary>
    /// Returns true if the crew member is available and certified for a given role
    /// </summary>
    Task<bool> IsAvailableAsync(Guid crewMemberId, string requiredRole);

    /// <summary>
    /// Finds all available crew with a specific role and/or certification
    /// </summary>
    Task<IEnumerable<CrewMember>> FindAvailableAsync(string role, string? certification = null);
    
    /// <summary>
    /// Finds all crew members that are available for a specific mission
    /// </summary>
    /// <param name="missionId"></param>
    /// <returns></returns>
    Task<IEnumerable<CrewMember>> FindByAssignedMissionAsync(Guid missionId);
    
    /// <summary>
    /// Creates or updates a crew member in the read model
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    Task CreateOrUpdateAsync(CrewMember member);

}
