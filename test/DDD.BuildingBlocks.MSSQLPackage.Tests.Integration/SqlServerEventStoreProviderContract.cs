using System;
using DDD.BuildingBlocks.Core.Persistence.Storage;
using DDD.BuildingBlocks.Tests.Abstracts;
using Xunit;

namespace DDD.BuildingBlocks.MSSQLPackage.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerEventStoreProviderContract(SqlServerContainerFixture fixture)
    : EventStoreProviderContract
{
    protected override IEventStoreProvider CreateProvider()
    {
        var options = new SqlServerEventStoreOptions { Schema = $"contract_{Guid.NewGuid():N}" };
        new SqlServerEventStoreMigrator(fixture.ConnectionString, options)
            .MigrateAsync(default)
            .GetAwaiter()
            .GetResult();
        return new SqlServerEventStoreProvider(fixture.ConnectionString, options);
    }
}
