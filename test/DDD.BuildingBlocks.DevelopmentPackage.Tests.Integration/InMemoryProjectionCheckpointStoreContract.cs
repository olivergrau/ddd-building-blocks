using DDD.BuildingBlocks.Core.Projection;
using DDD.BuildingBlocks.DevelopmentPackage.Projection;
using DDD.BuildingBlocks.Tests.Abstracts;

namespace DDD.BuildingBlocks.DevelopmentPackage.Tests.Integration;

public sealed class InMemoryProjectionCheckpointStoreContract : ProjectionCheckpointStoreContract
{
    protected override IProjectionCheckpointStore CreateStore() => new InMemoryProjectionCheckpointStore();
}
