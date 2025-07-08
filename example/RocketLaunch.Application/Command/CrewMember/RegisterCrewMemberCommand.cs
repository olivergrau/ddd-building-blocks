namespace RocketLaunch.Application.Command;

using DDD.BuildingBlocks.Core.Commanding;
using RocketLaunch.SharedKernel.Enums;

/// <summary>
/// Command to register a new crew member.
///
/// Preconditions:
/// - CrewMemberId must be unique.
///
/// Side Effects:
/// - Emits CrewMemberAssigned event with Available status (implicit from creation).
/// </summary>
public class RegisterCrewMemberCommand : Command
{
    public Guid CrewMemberId { get; }
    public string Name { get; }
    public CrewRole Role { get; }
    public IReadOnlyCollection<string> Certifications { get; }

    public RegisterCrewMemberCommand(
        Guid crewMemberId,
        string name,
        CrewRole role,
        IEnumerable<string> certifications)
        : base(crewMemberId.ToString(), -1)
    {
        CrewMemberId  = crewMemberId;
        Name          = name ?? throw new ArgumentNullException(nameof(name));
        Role          = role;
        Certifications = new List<string>(certifications ?? throw new ArgumentNullException(nameof(certifications))).AsReadOnly();

        Mode = AggregateSourcingMode.Create;
    }
}
