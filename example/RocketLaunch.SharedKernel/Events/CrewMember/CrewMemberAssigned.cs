// Domain/Events/CrewMember/AssignCrewMember.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.SharedKernel.Events.CrewMember;

[DomainEventType]
public sealed class CrewMemberAssigned : DomainEvent
{
    private const int CurrentClassVersion = 1;

    public CrewMemberId CrewMemberId { get; }

    public CrewMemberAssigned(CrewMemberId crewMemberId, int targetVersion = -1)
        : base(crewMemberId.Value.ToString(), targetVersion, CurrentClassVersion)
    {
        CrewMemberId = crewMemberId;
    }
}
