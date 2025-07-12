namespace RocketLaunch.Application.Command.CrewMember;

/// <summary>
/// Command to mark a crew member as assigned.
///
/// Preconditions:
/// - Crew member must be in Available status.
///
/// Side Effects:
/// - Emits CrewMemberAssigned domain event.
/// </summary>
public class AssignCrewMemberCommand : DDD.BuildingBlocks.Core.Commanding.Command
{
    public Guid CrewMemberId { get; }

    public AssignCrewMemberCommand(Guid crewMemberId)
        : base(crewMemberId.ToString(), -1)
    {
        CrewMemberId = crewMemberId;
    }
}
