using RocketLaunch.ReadModel.Core.Model;

namespace RocketLaunch.ReadModel.Core.Service;

public interface ICrewMemberService
{
    CrewMember? GetById(Guid crewMemberId);
    IEnumerable<CrewMember> GetAll();

    /// <summary>
    /// Returns true if the crew member is available and certified for a given role
    /// </summary>
    bool IsAvailable(Guid crewMemberId, string requiredRole);

    /// <summary>
    /// Finds all available crew with a specific role and/or certification
    /// </summary>
    IEnumerable<CrewMember> FindAvailable(string role, string? certification = null);
    
    /// <summary>
    /// Finds all crew members that are available for a specific mission
    /// </summary>
    /// <param name="missionId"></param>
    /// <returns></returns>
    IEnumerable<CrewMember> FindByAssignedMission(Guid missionId);
    
    /// <summary>
    /// Creates or updates a crew member in the read model
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    Task CreateOrUpdateAsync(CrewMember member);

}
