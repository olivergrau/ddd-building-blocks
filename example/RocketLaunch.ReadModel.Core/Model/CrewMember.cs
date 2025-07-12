namespace RocketLaunch.ReadModel.Core.Model;

public class CrewMember
{
    public Guid CrewMemberId { get; set; }
    public string Name { get; set; } = default!;
    public string Role { get; set; } = default!; // Commander, Pilot, etc.
    public List<string> CertificationLevels { get; set; } = [];

    public CrewMemberStatus Status { get; set; } = CrewMemberStatus.Unknown;

    public Guid? AssignedMissionId { get; set; }
}

public enum CrewMemberStatus
{
    Unknown,
    Available,
    Assigned,
    Unavailable
}
