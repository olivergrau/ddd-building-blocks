using System;
using DDD.BuildingBlocks.Core.Persistence.Storage;
using DDD.BuildingBlocks.Tests.Abstracts;
using Xunit;

namespace DDD.BuildingBlocks.MSSQLPackage.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerSnapshotStoreProviderContract(SqlServerContainerFixture fixture)
    : SnapshotStoreProviderContract
{
    protected override ISnapshotStoreProvider CreateProvider(int frequency = 10)
    {
        var options = new SqlServerEventStoreOptions { Schema = $"snapshot_{Guid.NewGuid():N}" };
        new SqlServerEventStoreMigrator(fixture.ConnectionString, options).MigrateAsync(default).GetAwaiter().GetResult();
        return new SqlServerSnapshotStoreProvider(fixture.ConnectionString, frequency, options);
    }
}
