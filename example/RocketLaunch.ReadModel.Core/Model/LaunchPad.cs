namespace RocketLaunch.ReadModel.Core.Model;

public class LaunchPad
{
    public Guid LaunchPadId { get; set; }
    public string PadName { get; set; } = default!;
    public string Location { get; set; } = default!;
    public List<string> SupportedRocketTypes { get; set; } = [];

    public LaunchPadStatus Status { get; set; } = LaunchPadStatus.Unknown;

    public List<ScheduledLaunchWindow> OccupiedWindows { get; set; } = [];
}

public enum LaunchPadStatus
{
    Unknown,
    Available,
    Occupied,
    UnderMaintenance
}

public class ScheduledLaunchWindow
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public Guid MissionId { get; set; }
}
