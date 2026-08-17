using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Xunit;

namespace DDD.BuildingBlocks.PostgreSQLPackage.Tests.Integration;

[Collection(PostgreSqlCollection.Name)]
public sealed class PostgreSqlEventStoreMigratorShould(PostgreSqlContainerFixture fixture)
{
    [Fact]
    public async Task Apply_the_initial_migration_idempotently()
    {
        var options = new PostgreSqlEventStoreOptions { Schema = $"migration_{Guid.NewGuid():N}" };
        var migrator = new PostgreSqlEventStoreMigrator(fixture.DataSource, options);

        await migrator.MigrateAsync(CancellationToken.None);
        await migrator.MigrateAsync(CancellationToken.None);

        await using var command = fixture.DataSource.CreateCommand(
            $"SELECT max(version) FROM {options.Schema}.schema_versions;");
        var version = await command.ExecuteScalarAsync(CancellationToken.None);
        Assert.Equal(3, version);
    }
}
