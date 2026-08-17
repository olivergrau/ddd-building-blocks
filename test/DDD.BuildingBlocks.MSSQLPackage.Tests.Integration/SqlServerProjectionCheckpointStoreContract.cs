using System;
using DDD.BuildingBlocks.Core.Projection;
using DDD.BuildingBlocks.Tests.Abstracts;
using Xunit;

namespace DDD.BuildingBlocks.MSSQLPackage.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerProjectionCheckpointStoreContract(SqlServerContainerFixture fixture)
    : ProjectionCheckpointStoreContract
{
    protected override IProjectionCheckpointStore CreateStore()
    {
        var options = new SqlServerEventStoreOptions { Schema = $"projection_{Guid.NewGuid():N}" };
        new SqlServerEventStoreMigrator(fixture.ConnectionString, options).MigrateAsync(default).GetAwaiter().GetResult();
        return new SqlServerProjectionCheckpointStore(fixture.ConnectionString, options);
    }
}
