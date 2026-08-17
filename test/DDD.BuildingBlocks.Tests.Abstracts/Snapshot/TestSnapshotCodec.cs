using DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;

namespace DDD.BuildingBlocks.Tests.Abstracts.Snapshot;

public static class TestSnapshotCodec
{
    public static ISnapshotCodec Create() => new SystemTextJsonSnapshotCodec(
        new SnapshotTypeRegistry().Register<OrderSnapshot>("order.snapshot"));
}
