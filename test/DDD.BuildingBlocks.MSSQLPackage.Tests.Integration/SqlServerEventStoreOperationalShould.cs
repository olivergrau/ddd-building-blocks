using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Projection;
using Microsoft.Data.SqlClient;
using Xunit;

namespace DDD.BuildingBlocks.MSSQLPackage.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerEventStoreOperationalShould(SqlServerContainerFixture fixture)
{
    [Fact]
    public async Task Preserve_a_checkpoint_safe_feed_under_parallel_stream_appends()
    {
        var provider = await CreateProviderAsync("parallel");
        var appends = Enumerable.Range(0, 50)
            .Select(index => provider.AppendAsync(
                $"stream-{index}", "order", -1, [CreateEnvelope($"stream-{index}", 0)], CancellationToken.None));

        await Task.WhenAll(appends);
        var feed = await provider.ReadCommittedFeedAsync(-1, 100, CancellationToken.None);

        Assert.Equal(50, feed.Count);
        Assert.Equal(Enumerable.Range(0, 50).Select(value => (long)value), feed.Select(item => item.GlobalPosition!.Value));
    }

    [Fact]
    public async Task Append_a_large_tvp_batch_and_payload_in_one_transaction()
    {
        var provider = await CreateProviderAsync("large_batch");
        var value = new string('x', 64 * 1024);
        var events = Enumerable.Range(0, 100)
            .Select(version => CreateEnvelope("large-stream", version, value))
            .ToArray();

        var append = await provider.AppendAsync("large-stream", "order", -1, events, CancellationToken.None);
        var stored = await provider.ReadStreamAsync("large-stream", "order", 0, 100, CancellationToken.None);

        Assert.Equal(99, append.CurrentStreamVersion);
        Assert.Equal(100, stored.Count);
        Assert.All(stored, item => Assert.Equal(value.Length, item.Payload.GetProperty("value").GetString()!.Length));
    }

    [Fact]
    public async Task Roll_back_the_stream_and_global_position_allocator_after_a_failed_batch()
    {
        var provider = await CreateProviderAsync("rollback");
        var duplicateId = Guid.NewGuid();
        await provider.AppendAsync(
            "first", "order", -1, [CreateEnvelope("first", 0, eventId: duplicateId)], CancellationToken.None);

        await Assert.ThrowsAsync<DuplicateEventIdException>(() => provider.AppendAsync(
            "rejected", "order", -1, [CreateEnvelope("rejected", 0, eventId: duplicateId)], CancellationToken.None));
        var accepted = await provider.AppendAsync(
            "second", "order", -1, [CreateEnvelope("second", 0)], CancellationToken.None);

        Assert.Equal(1, accepted.FirstGlobalPosition);
        Assert.Empty(await provider.ReadStreamAsync("rejected", "order", 0, 10, CancellationToken.None));
    }

    [Fact]
    public async Task Roll_back_an_interrupted_database_session()
    {
        var schema = $"termination_{Guid.NewGuid():N}";
        var provider = await CreateProviderAsync(schema, exactSchema: true);
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(CancellationToken.None);
        await using (var mutate = new SqlCommand($$"""
            INSERT INTO [{{schema}}].[EventStreams] ([AggregateType], [StreamId], [CurrentVersion])
            VALUES (N'order', N'interrupted', -1);
            UPDATE [{{schema}}].[EventStoreState] SET [NextGlobalPosition] = 1 WHERE [Singleton] = 1;
            """, connection, transaction))
        {
            await mutate.ExecuteNonQueryAsync(CancellationToken.None);
        }

        int processId;
        await using (var backend = new SqlCommand("SELECT @@SPID;", connection, transaction))
        {
            processId = Convert.ToInt32(
                await backend.ExecuteScalarAsync(CancellationToken.None),
                System.Globalization.CultureInfo.InvariantCulture);
        }

        await using (var terminator = new SqlConnection(fixture.ConnectionString))
        {
            await terminator.OpenAsync(CancellationToken.None);
            await using var terminate = new SqlCommand($"KILL {processId};", terminator);
            await terminate.ExecuteNonQueryAsync(CancellationToken.None);
        }

        var accepted = await provider.AppendAsync(
            "after-interruption", "order", -1, [CreateEnvelope("after-interruption", 0)], CancellationToken.None);

        Assert.Equal(0, accepted.FirstGlobalPosition);
        Assert.Empty(await provider.ReadStreamAsync("interrupted", "order", 0, 10, CancellationToken.None));
    }

    [Fact]
    public async Task Restore_a_native_backup_and_continue_the_feed()
    {
        var schema = $"backup_{Guid.NewGuid():N}";
        var provider = await CreateProviderAsync(schema, exactSchema: true);
        await provider.AppendAsync("first", "order", -1, [CreateEnvelope("first", 0)], CancellationToken.None);
        var suffix = Guid.NewGuid().ToString("N");
        var archive = $"/var/opt/mssql/data/event_store_{suffix}.bak";
        var restoredDatabase = $"restored_{suffix}";

        await ExecuteNonQueryAsync(
            "BACKUP DATABASE [ddd_building_blocks] TO DISK = @Archive WITH COPY_ONLY, INIT;",
            new SqlParameter("@Archive", SqlDbType.NVarChar, 4000) { Value = archive });
        var logicalFiles = await ReadLogicalFilesAsync(archive);

        var masterConnectionString = new SqlConnectionStringBuilder(fixture.ConnectionString)
        {
            InitialCatalog = "master",
        }.ConnectionString;
        await using (var master = new SqlConnection(masterConnectionString))
        {
            await master.OpenAsync(CancellationToken.None);
            var restoreSql = $"""
                RESTORE DATABASE [{restoredDatabase}]
                FROM DISK = @Archive
                WITH MOVE @DataLogical TO @DataPath,
                     MOVE @LogLogical TO @LogPath;
                """;
            await using var restore = new SqlCommand(restoreSql, master);
            restore.Parameters.Add("@Archive", SqlDbType.NVarChar, 4000).Value = archive;
            restore.Parameters.Add("@DataLogical", SqlDbType.NVarChar, 128).Value = logicalFiles.Data;
            restore.Parameters.Add("@LogLogical", SqlDbType.NVarChar, 128).Value = logicalFiles.Log;
            restore.Parameters.Add("@DataPath", SqlDbType.NVarChar, 4000).Value = $"/var/opt/mssql/data/{restoredDatabase}.mdf";
            restore.Parameters.Add("@LogPath", SqlDbType.NVarChar, 4000).Value = $"/var/opt/mssql/data/{restoredDatabase}_log.ldf";
            await restore.ExecuteNonQueryAsync(CancellationToken.None);
        }

        var restoredConnectionString = new SqlConnectionStringBuilder(fixture.ConnectionString)
        {
            InitialCatalog = restoredDatabase,
        }.ConnectionString;
        var restored = new SqlServerEventStoreProvider(
            restoredConnectionString, new SqlServerEventStoreOptions { Schema = schema });
        var feed = await restored.ReadCommittedFeedAsync(-1, 10, CancellationToken.None);
        var append = await restored.AppendAsync(
            "second", "order", -1, [CreateEnvelope("second", 0)], CancellationToken.None);

        Assert.Single(feed);
        Assert.Equal(1, append.FirstGlobalPosition);
    }

    [Fact]
    public async Task Commit_read_model_changes_and_checkpoint_in_the_same_transaction()
    {
        var schema = $"projection_atomic_{Guid.NewGuid():N}";
        var options = new SqlServerEventStoreOptions { Schema = schema };
        await new SqlServerEventStoreMigrator(fixture.ConnectionString, options).MigrateAsync(CancellationToken.None);
        await ExecuteNonQueryAsync($"CREATE TABLE [{schema}].[ReadModel] ([Position] bigint PRIMARY KEY);");

        var checkpoints = new SqlServerProjectionCheckpointStore(fixture.ConnectionString, options);
        var key = new ProjectionKey("atomic", 1);
        await checkpoints.ProcessAsync(key, CreateCommittedEnvelope(0), InsertReadModel, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => checkpoints.ProcessAsync(
            key, CreateCommittedEnvelope(1), async (context, token) =>
            {
                await InsertReadModel(context, token);
                throw new InvalidOperationException("rollback projection");
            }, CancellationToken.None));

        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using var count = new SqlCommand($"SELECT COUNT_BIG(*) FROM [{schema}].[ReadModel];", connection);
        Assert.Equal(1L, await count.ExecuteScalarAsync(CancellationToken.None));
        Assert.Equal(0, (await checkpoints.GetAsync(key, CancellationToken.None)).LastProcessedPosition);

        async Task InsertReadModel(ProjectionTransactionContext context, CancellationToken token)
        {
            await using var command = new SqlCommand(
                $"INSERT INTO [{schema}].[ReadModel] ([Position]) SELECT COUNT_BIG(*) FROM [{schema}].[ReadModel];",
                (SqlConnection)context.Connection!,
                (SqlTransaction)context.Transaction!);
            await command.ExecuteNonQueryAsync(token);
        }
    }

    private async Task<(string Data, string Log)> ReadLogicalFilesAsync(string archive)
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using var command = new SqlCommand("RESTORE FILELISTONLY FROM DISK = @Archive;", connection);
        command.Parameters.Add("@Archive", SqlDbType.NVarChar, 4000).Value = archive;
        await using var reader = await command.ExecuteReaderAsync(CancellationToken.None);
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        while (await reader.ReadAsync(CancellationToken.None))
        {
            files.Add(reader.GetString(2), reader.GetString(0));
        }

        return (files["D"], files["L"]);
    }

    private async Task ExecuteNonQueryAsync(string sql, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }

    private async Task<SqlServerEventStoreProvider> CreateProviderAsync(string prefix, bool exactSchema = false)
    {
        var schema = exactSchema ? prefix : $"{prefix}_{Guid.NewGuid():N}";
        var options = new SqlServerEventStoreOptions { Schema = schema };
        await new SqlServerEventStoreMigrator(fixture.ConnectionString, options).MigrateAsync(CancellationToken.None);
        return new SqlServerEventStoreProvider(fixture.ConnectionString, options);
    }

    private static EventEnvelope CreateEnvelope(
        string streamId,
        long streamVersion,
        string value = "value",
        Guid? eventId = null)
    {
        using var payload = JsonDocument.Parse($"{{\"value\":{JsonSerializer.Serialize(value)}}}");
        return new EventEnvelope(
            eventId ?? Guid.NewGuid(), streamId, "order", streamVersion, null, "order.changed", 1,
            DateTimeOffset.UtcNow, null, null, null, null, null, null, payload.RootElement);
    }

    private static EventEnvelope CreateCommittedEnvelope(long position)
    {
        using var payload = JsonDocument.Parse("{\"value\":1}");
        return new EventEnvelope(
            Guid.NewGuid(), "stream", "order", position, position, "order.changed", 1,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, null, null, null, payload.RootElement);
    }
}
