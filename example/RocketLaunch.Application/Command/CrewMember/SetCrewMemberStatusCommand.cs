namespace RocketLaunch.Application.Command;

using DDD.BuildingBlocks.Core.Commanding;
using RocketLaunch.SharedKernel.Enums;

/// <summary>
/// Command to set the status of a crew member.
/// </summary>
public class SetCrewMemberStatusCommand : Command
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
