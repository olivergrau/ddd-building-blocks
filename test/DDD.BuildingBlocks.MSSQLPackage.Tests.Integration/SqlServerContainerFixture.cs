using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace DDD.BuildingBlocks.MSSQLPackage.Tests.Integration;

public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder(
        "mcr.microsoft.com/mssql/server:2025-CU7-ubuntu-22.04")
        .WithPassword("Ddd.BuildingBlocks_2026!")
        .Build();

    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);
        await using (var connection = new SqlConnection(_container.GetConnectionString()))
        {
            await connection.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await using var command = new SqlCommand("CREATE DATABASE [ddd_building_blocks];", connection);
            await command.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
        }

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "ddd_building_blocks",
        }.ConnectionString;
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerContainerFixture>
{
    public const string Name = "SQL Server event store";
}
