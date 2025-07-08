// Domain/Events/CrewMember/SetCertificationForCrewMember.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.SharedKernel.Events.CrewMember;

[DomainEventType]
public sealed class CrewMemberCertificationSet : DomainEvent
{
    private const int CurrentClassVersion = 1;

    public CrewMemberId CrewMemberId { get; }
    public IReadOnlyCollection<string> Certifications { get; }

    public CrewMemberCertificationSet(
        CrewMemberId crewMemberId,
        IEnumerable<string> certifications,
        int targetVersion = -1)
        : base(crewMemberId.Value.ToString(), targetVersion, CurrentClassVersion)
    {
        CrewMemberId = crewMemberId;
        Certifications = new List<string>(certifications).AsReadOnly();
    }
}
