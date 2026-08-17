using DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;

namespace RocketLaunch.SharedKernel.Snapshots;

public static class RocketLaunchSnapshotCodec
{
    public static ISnapshotCodec Create() => new SystemTextJsonSnapshotCodec(
        new SnapshotTypeRegistry().Register<MissionSnapshot>("mission.snapshot"));
}
