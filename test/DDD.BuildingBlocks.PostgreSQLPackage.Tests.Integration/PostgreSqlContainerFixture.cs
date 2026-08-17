using System.Threading.Tasks;
using Npgsql;
using Testcontainers.PostgreSql;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace DDD.BuildingBlocks.PostgreSQLPackage.Tests.Integration;

public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.1-alpine")
        .WithDatabase("ddd_building_blocks")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public Task<ExecResult> ExecAsync(params string[] command) => _container.ExecAsync(command);

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);
        DataSource = NpgsqlDataSource.Create(_container.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        if (DataSource is not null)
        {
            await DataSource.DisposeAsync().ConfigureAwait(false);
        }

        await _container.DisposeAsync().ConfigureAwait(false);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlContainerFixture>
{
    public const string Name = "PostgreSQL event store";
}
