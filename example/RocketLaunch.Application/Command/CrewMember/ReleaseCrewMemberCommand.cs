namespace RocketLaunch.Application.Command;

/// <summary>
/// Command to release a crew member from assignment.
///
/// Side Effects:
/// - Emits CrewMemberReleased domain event.
/// </summary>
public class ReleaseCrewMemberCommand : DDD.BuildingBlocks.Core.Commanding.Command
{
    public Guid CrewMemberId { get; }

    public ReleaseCrewMemberCommand(Guid crewMemberId)
        : base(crewMemberId.ToString(), -1)
    {
        CrewMemberId = crewMemberId;
    }
}
