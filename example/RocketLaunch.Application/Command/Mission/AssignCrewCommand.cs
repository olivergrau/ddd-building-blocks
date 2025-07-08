namespace RocketLaunch.Application.Command;

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
public class AssignCrewCommand : DDD.BuildingBlocks.Core.Commanding.Command
{
    public Guid MissionId { get; }
    public IReadOnlyCollection<Guid> CrewMemberIds { get; }

    public AssignCrewCommand(Guid missionId, IEnumerable<Guid> crewMemberIds)
        : base(missionId.ToString(), -1)
    {
        MissionId     = missionId;
        CrewMemberIds = new List<Guid>(crewMemberIds ?? throw new ArgumentNullException(nameof(crewMemberIds))).AsReadOnly();
    }
}