using RocketLaunch.SharedKernel.Enums;

namespace RocketLaunch.Application.Command.CrewMember;

/// <summary>
/// Command to set the status of a crew member.
/// </summary>
public class SetCrewMemberStatusCommand : DDD.BuildingBlocks.Core.Commanding.Command
{
    public Guid CrewMemberId { get; }
    public CrewMemberStatus Status { get; }

    public SetCrewMemberStatusCommand(Guid crewMemberId, CrewMemberStatus status)
        : base(crewMemberId.ToString(), -1)
    {
        CrewMemberId = crewMemberId;
        Status       = status;
    }
}
