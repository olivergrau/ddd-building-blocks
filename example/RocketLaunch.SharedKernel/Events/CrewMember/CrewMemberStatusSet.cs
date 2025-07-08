// Domain/Events/CrewMember/SetStatusForCrewMember.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.SharedKernel.Events.CrewMember;

[DomainEventType]
public sealed class CrewMemberStatusSet : DomainEvent
{
    private const int CurrentClassVersion = 1;

    public CrewMemberId CrewMemberId { get; }
    public CrewMemberStatus Status { get; }

    public CrewMemberStatusSet(CrewMemberId crewMemberId, CrewMemberStatus status, int targetVersion = -1)
        : base(crewMemberId.Value.ToString(), targetVersion, CurrentClassVersion)
    {
        CrewMemberId = crewMemberId;
        Status = status;
    }
}
