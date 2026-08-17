using DDD.BuildingBlocks.Core.Persistence.Storage;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using DDD.BuildingBlocks.Tests.Abstracts;

namespace DDD.BuildingBlocks.DevelopmentPackage.Tests.Integration;

public sealed class InMemoryEventStoreProviderContract : EventStoreProviderContract
{
    protected override IEventStoreProvider CreateProvider() => new InMemoryEventStoreProvider();
}
