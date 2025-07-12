namespace RocketLaunch.Application.Command.CrewMember;

/// <summary>
/// Command to update the certifications of a crew member.
/// </summary>
public class SetCrewMemberCertificationsCommand : DDD.BuildingBlocks.Core.Commanding.Command
{
    public Guid CrewMemberId { get; }
    public IReadOnlyCollection<string> Certifications { get; }

    public SetCrewMemberCertificationsCommand(Guid crewMemberId, IEnumerable<string> certifications)
        : base(crewMemberId.ToString(), -1)
    {
        CrewMemberId   = crewMemberId;
        Certifications = new List<string>(certifications ?? throw new ArgumentNullException(nameof(certifications))).AsReadOnly();
    }
}
