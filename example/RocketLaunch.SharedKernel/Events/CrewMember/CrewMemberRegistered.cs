// Domain/Events/CrewMember/CrewMemberRegistered.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.SharedKernel.Events.CrewMember;

[DomainEventType]
public sealed class CrewMemberRegistered : DomainEvent
{
    private const int CurrentClassVersion = 1;

    public CrewMemberId CrewMemberId { get; }
    public string Name { get; }
    public CrewRole Role { get; }
    public IReadOnlyCollection<string> Certifications { get; }

    public CrewMemberRegistered(
        CrewMemberId crewMemberId,
        string name,
        CrewRole role,
        IEnumerable<string> certifications,
        int targetVersion = -1)
        : base(crewMemberId.Value.ToString(), targetVersion, CurrentClassVersion)
    {
        CrewMemberId = crewMemberId;
        Name = name;
        Role = role;
        Certifications = new List<string>(certifications).AsReadOnly();
    }
}
