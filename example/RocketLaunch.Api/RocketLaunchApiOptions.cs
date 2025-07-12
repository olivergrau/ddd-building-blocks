namespace RocketLaunch.Api;

public sealed class RocketLaunchApiOptions
{
    public string WorkerId { get; set; } = "rocket-launch-worker";
    public string ReadModelAssemblyName { get; set; } = "RocketLaunch.ReadModel.Core";
    public string SnapshotPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "mission.snapshots.dump");
    public int SnapshotThreshold { get; set; } = 5;
    public int GlobalTriggerTimeoutInMilliseconds { get; set; } = 1000;
}
