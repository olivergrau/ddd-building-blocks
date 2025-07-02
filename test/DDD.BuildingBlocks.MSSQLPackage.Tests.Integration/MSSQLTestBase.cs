
#pragma warning disable CS0618
namespace DDD.BuildingBlocks.MSSQLPackage.Tests.Integration;

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Core.Persistence.Repository;
using Core.Persistence.SnapshotSupport;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Provider;
using Service;
using Testcontainers.MsSql;
using Xunit;
using AggregateInformationService = Service.AggregateInformationService;

[SuppressMessage("Design", "CA1051:Do not declare visible instance fields")]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
#pragma warning disable CA1063
public abstract class MSSQLTestBase : IAsyncLifetime
#pragma warning restore CA1063
{
    protected string? ConnectionString;

    public readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .Build();

    protected EventSourcingRepository GetRepositoryWithoutSnapshotProvider() =>
        new(new EventStorageProvider(new EventStorageProviderSettings { ConnectionString = ConnectionString! }.AsOptionsMonitor()));

    protected EventSourcingRepository GetRepositoryWithActivatedSnapshotProvider(int snapshotFrequency) =>
        new(
            new EventStorageProvider(new EventStorageProviderSettings { ConnectionString = ConnectionString! }.AsOptionsMonitor()),
            new SnapshotStorageProvider(new SnapshotStorageProviderSettings { ConnectionString = ConnectionString!, SnapshotFrequency = snapshotFrequency}.AsOptionsMonitor()));

    protected ISnapshotCreationService GetSnapshotCreationServiceBasedOnANonSnapshotEnabledRepository() =>
        new SnapshotCreationService(GetRepositoryWithoutSnapshotProvider(), new AggregateInformationService(
            new AggregateInformationServiceSettings { ConnectionString = ConnectionString! }.AsOptionsMonitor(), new NullLogger<AggregateInformationService>()),
            new NullLoggerFactory());

    protected ISnapshotCreationService GetSnapshotCreationServiceBasedOnASnapshotEnabledRepository(int snapshotFrequency = 5) =>
        new SnapshotCreationService(GetRepositoryWithActivatedSnapshotProvider(snapshotFrequency), new AggregateInformationService(
                new AggregateInformationServiceSettings { ConnectionString = ConnectionString! }.AsOptionsMonitor(), new NullLogger<AggregateInformationService>()),
            new NullLoggerFactory());

    protected async Task PrepareEventWithInvalidType()
    {
        const string invalidType = "InvalidTypeHint";
        await using var connection = new SqlConnection(ConnectionString);
        await using var command = connection.CreateCommand();
        await connection.OpenAsync();
        command.CommandText = "UPDATE dbo.EVENTS SET TYPE = @TYPE WHERE VERSION = 0";
        command.Parameters.Add(invalidType.ToSqlParameter("@TYPE"));
        await command.ExecuteNonQueryAsync();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        var sqlFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "DDL.sql");
        var script = await File.ReadAllTextAsync(sqlFilePath);
        await _dbContainer.ExecScriptAsync(script);
        ConnectionString = _dbContainer.GetConnectionString() + ";TrustServerCertificate=true;MultipleActiveResultSets=true;";
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}
