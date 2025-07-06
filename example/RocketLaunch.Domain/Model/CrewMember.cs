// Domain/Aggregates/CrewMember.cs

using DDD.BuildingBlocks.Core.Domain;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Domain.Model;

public class CrewMember : AggregateRoot<CrewMemberId>
{
    public CrewMember(CrewMemberId id, string name, CrewRole role, IEnumerable<string> certifications)
        : base(id)
    {
        Name = name;
        Role = role;
        Certifications = new List<string>(certifications);
        Status = CrewMemberStatus.Available;
    }

    // For rehydration
    private CrewMember() : base(default!) { }

    public string Name { get; private set; } = null!;
    public CrewRole Role { get; private set; }
    public IReadOnlyList<string> Certifications { get; private set; } = null!;
    public CrewMemberStatus Status { get; private set; }

    public void Assign()
    {
        if (Status != CrewMemberStatus.Available)
            throw new Exception("Crew member is not available");
        Status = CrewMemberStatus.Assigned;
    }

    public void Release()
    {
        Status = CrewMemberStatus.Available;
    }

    public void SetCertifications(IEnumerable<string> certifications)
    {
        Certifications = new List<string>(certifications ?? throw new ArgumentNullException(nameof(certifications)));
    }

    public void SetStatus(CrewMemberStatus status)
    {
        Status = status;
    }

    protected override CrewMemberId GetIdFromStringRepresentation(string value)
        => new(new Guid(value));
}
