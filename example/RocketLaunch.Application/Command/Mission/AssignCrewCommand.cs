namespace RocketLaunch.Application.Command.Mission;

/// <summary>
/// Command to assign crew members to a mission.
/// 
/// Preconditions:
/// - Mission must be crewed.
/// - Rocket and launch pad must already be assigned.
/// - All crew members must be certified and available.
/// 
/// Side Effects:
/// - Emits CrewAssigned domain event.
/// </summary>
public class AssignCrewCommand(Guid missionId, IEnumerable<Guid> crewMemberIds)
    : DDD.BuildingBlocks.Core.Commanding.Command(missionId.ToString(), -1)
{
    public Guid MissionId { get; } = missionId;
    public IReadOnlyCollection<Guid> CrewMemberIds { get; } = new List<Guid>(crewMemberIds ?? throw new ArgumentNullException(nameof(crewMemberIds))).AsReadOnly();
}