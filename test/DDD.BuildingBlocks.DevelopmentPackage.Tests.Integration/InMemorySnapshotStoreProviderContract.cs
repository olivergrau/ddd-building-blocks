using DDD.BuildingBlocks.Core.Persistence.Storage;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using DDD.BuildingBlocks.Tests.Abstracts;

namespace DDD.BuildingBlocks.DevelopmentPackage.Tests.Integration;

public sealed class InMemorySnapshotStoreProviderContract : SnapshotStoreProviderContract
{
    protected override ISnapshotStoreProvider CreateProvider(int frequency = 10) =>
        new InMemorySnapshotStoreProvider(frequency);
}
