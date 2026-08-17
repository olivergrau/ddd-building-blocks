using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Xunit;

namespace DDD.BuildingBlocks.MSSQLPackage.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerEventStoreMigratorShould(SqlServerContainerFixture fixture)
{
    [Fact]
    public async Task Apply_the_initial_migration_idempotently()
    {
        var options = new SqlServerEventStoreOptions { Schema = $"migration_{Guid.NewGuid():N}" };
        var migrator = new SqlServerEventStoreMigrator(fixture.ConnectionString, options);

        await migrator.MigrateAsync(CancellationToken.None);
        await migrator.MigrateAsync(CancellationToken.None);

        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using var command = new SqlCommand(
            $"SELECT MAX([Version]) FROM [{options.Schema}].[SchemaVersions];", connection);
        Assert.Equal(3, await command.ExecuteScalarAsync(CancellationToken.None));
    }
}
