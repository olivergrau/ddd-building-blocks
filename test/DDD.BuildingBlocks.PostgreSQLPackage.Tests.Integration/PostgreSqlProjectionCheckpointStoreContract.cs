using System;
using DDD.BuildingBlocks.Core.Projection;
using DDD.BuildingBlocks.Tests.Abstracts;
using Xunit;

namespace DDD.BuildingBlocks.PostgreSQLPackage.Tests.Integration;

[Collection(PostgreSqlCollection.Name)]
public sealed class PostgreSqlProjectionCheckpointStoreContract(PostgreSqlContainerFixture fixture)
    : ProjectionCheckpointStoreContract
{
    protected override IProjectionCheckpointStore CreateStore()
    {
        var options = new PostgreSqlEventStoreOptions { Schema = $"projection_{Guid.NewGuid():N}" };
        new PostgreSqlEventStoreMigrator(fixture.DataSource, options).MigrateAsync(default).GetAwaiter().GetResult();
        return new PostgreSqlProjectionCheckpointStore(fixture.DataSource, options);
    }
}
