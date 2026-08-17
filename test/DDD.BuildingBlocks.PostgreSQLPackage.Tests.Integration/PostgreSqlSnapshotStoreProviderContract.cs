using System;
using DDD.BuildingBlocks.Core.Persistence.Storage;
using DDD.BuildingBlocks.Tests.Abstracts;
using Xunit;

namespace DDD.BuildingBlocks.PostgreSQLPackage.Tests.Integration;

[Collection(PostgreSqlCollection.Name)]
public sealed class PostgreSqlSnapshotStoreProviderContract(PostgreSqlContainerFixture fixture)
    : SnapshotStoreProviderContract
{
    protected override ISnapshotStoreProvider CreateProvider(int frequency = 10)
    {
        var options = new PostgreSqlEventStoreOptions { Schema = $"snapshot_{Guid.NewGuid():N}" };
        new PostgreSqlEventStoreMigrator(fixture.DataSource, options).MigrateAsync(default).GetAwaiter().GetResult();
        return new PostgreSqlSnapshotStoreProvider(fixture.DataSource, frequency, options);
    }
}
