using System;
using DDD.BuildingBlocks.Core.Persistence.Storage;
using DDD.BuildingBlocks.Tests.Abstracts;
using Xunit;

namespace DDD.BuildingBlocks.PostgreSQLPackage.Tests.Integration;

[Collection(PostgreSqlCollection.Name)]
public sealed class PostgreSqlEventStoreProviderContract(PostgreSqlContainerFixture fixture)
    : EventStoreProviderContract
{
    protected override IEventStoreProvider CreateProvider()
    {
        var options = new PostgreSqlEventStoreOptions
        {
            Schema = $"contract_{Guid.NewGuid():N}",
        };
        new PostgreSqlEventStoreMigrator(fixture.DataSource, options)
            .MigrateAsync(default)
            .GetAwaiter()
            .GetResult();
        return new PostgreSqlEventStoreProvider(fixture.DataSource, options);
    }
}
