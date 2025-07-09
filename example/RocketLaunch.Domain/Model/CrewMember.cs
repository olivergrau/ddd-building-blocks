// Domain/Aggregates/CrewMember.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Domain;
using JetBrains.Annotations;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.Events.CrewMember;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Domain.Model;

public class CrewMember : AggregateRoot<CrewMemberId>
{
    public CrewMember(CrewMemberId id, string name, CrewRole role, IEnumerable<string> certifications)
        : base(id)
    {
        ApplyEvent(new CrewMemberRegistered(id, name, role, certifications));
    }

    // For rehydration
    public CrewMember() : base(default!) { }

    public string Name { get; private set; } = null!;
    public CrewRole Role { get; private set; }
    public IReadOnlyList<string> Certifications { get; private set; } = null!;
    public CrewMemberStatus Status { get; private set; }

    public void Assign()
    {
        if (Status != CrewMemberStatus.Available)
            throw new Exception("Crew member is not available");

        ApplyEvent(new CrewMemberAssigned(Id, CurrentVersion));
    }

    public void Release()
    {
        ApplyEvent(new CrewMemberReleased(Id, CurrentVersion));
    }

    public void SetCertifications(IEnumerable<string> certifications)
    {
        ApplyEvent(new CrewMemberCertificationSet(Id,
            certifications ?? throw new ArgumentNullException(nameof(certifications)),
            CurrentVersion));
    }

    public void SetStatus(CrewMemberStatus status)
    {
        ApplyEvent(new CrewMemberStatusSet(Id, status, CurrentVersion));
    }

    // Event handlers

    [UsedImplicitly]
    [InternalEventHandler]
    private void On(CrewMemberRegistered e)
    {
        Name = e.Name;
        Role = e.Role;
        Certifications = new List<string>(e.Certifications);
        Status = CrewMemberStatus.Available;
    }

    [UsedImplicitly]
    [InternalEventHandler]
    private void On(CrewMemberAssigned e)
    {
        Status = CrewMemberStatus.Assigned;
    }

    [UsedImplicitly]
    [InternalEventHandler]
    private void On(CrewMemberReleased e)
    {
        Status = CrewMemberStatus.Available;
    }

    [UsedImplicitly]
    [InternalEventHandler]
    private void On(CrewMemberCertificationSet e)
    {
        Certifications = new List<string>(e.Certifications);
    }

    [UsedImplicitly]
    [InternalEventHandler]
    private void On(CrewMemberStatusSet e)
    {
        Status = e.Status;
    }

    protected override CrewMemberId GetIdFromStringRepresentation(string value)
        => new(new Guid(value));
}
