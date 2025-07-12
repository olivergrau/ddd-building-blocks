using RocketLaunch.SharedKernel.Enums;

namespace RocketLaunch.ReadModel.Core.Model;

public class Mission
{
    public Guid MissionId { get; set; }
    public string Name { get; set; } = default!;
    public string TargetOrbit { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime LaunchWindowStart { get; set; }
    public DateTime LaunchWindowEnd { get; set; }

    public MissionStatus Status { get; set; } = MissionStatus.Planned;

    public Guid? AssignedRocketId { get; set; }
    public Guid? AssignedPadId { get; set; }
    public List<Guid> CrewMemberIds { get; set; } = [];
}
