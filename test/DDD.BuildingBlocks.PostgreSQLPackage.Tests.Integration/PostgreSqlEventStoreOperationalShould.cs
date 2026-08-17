using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Projection;
using Npgsql;
using Xunit;

namespace DDD.BuildingBlocks.PostgreSQLPackage.Tests.Integration;

[Collection(PostgreSqlCollection.Name)]
public sealed class PostgreSqlEventStoreOperationalShould(PostgreSqlContainerFixture fixture)
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
    public async Task Append_a_large_batch_and_payload_in_one_transaction()
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
        await using var connection = await fixture.DataSource.OpenConnectionAsync(CancellationToken.None);
        await using var transaction = await connection.BeginTransactionAsync(CancellationToken.None);
        await using (var mutate = new NpgsqlCommand($$"""
            INSERT INTO {{schema}}.event_streams (aggregate_type, stream_id, current_version)
            VALUES ('order', 'interrupted', -1);
            UPDATE {{schema}}.event_store_state SET next_global_position = 1 WHERE singleton = TRUE;
            """, connection, transaction))
        {
            await mutate.ExecuteNonQueryAsync(CancellationToken.None);
        }

        int processId;
        await using (var backend = new NpgsqlCommand("SELECT pg_backend_pid();", connection, transaction))
        {
            processId = (int)(await backend.ExecuteScalarAsync(CancellationToken.None))!;
        }

        await using (var terminate = fixture.DataSource.CreateCommand("SELECT pg_terminate_backend($1);"))
        {
            terminate.Parameters.AddWithValue(processId);
            Assert.True((bool)(await terminate.ExecuteScalarAsync(CancellationToken.None))!);
        }

        var accepted = await provider.AppendAsync(
            "after-interruption", "order", -1, [CreateEnvelope("after-interruption", 0)], CancellationToken.None);

        Assert.Equal(0, accepted.FirstGlobalPosition);
        Assert.Empty(await provider.ReadStreamAsync("interrupted", "order", 0, 10, CancellationToken.None));
    }

    [Fact]
    public async Task Restore_a_schema_level_backup_and_continue_the_feed()
    {
        var schema = $"backup_{Guid.NewGuid():N}";
        var provider = await CreateProviderAsync(schema, exactSchema: true);
        await provider.AppendAsync("first", "order", -1, [CreateEnvelope("first", 0)], CancellationToken.None);
        var archive = $"/tmp/{schema}.dump";

        var backup = await fixture.ExecAsync(
            "pg_dump", "--username=postgres", "--dbname=ddd_building_blocks", $"--schema={schema}",
            "--format=custom", $"--file={archive}");
        Assert.Equal(0, backup.ExitCode);

        await using (var drop = fixture.DataSource.CreateCommand($"DROP SCHEMA {schema} CASCADE;"))
        {
            await drop.ExecuteNonQueryAsync(CancellationToken.None);
        }

        var restore = await fixture.ExecAsync(
            "pg_restore", "--username=postgres", "--dbname=ddd_building_blocks", archive);
        Assert.Equal(0, restore.ExitCode);

        var restored = new PostgreSqlEventStoreProvider(
            fixture.DataSource, new PostgreSqlEventStoreOptions { Schema = schema });
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
        var options = new PostgreSqlEventStoreOptions { Schema = schema };
        await new PostgreSqlEventStoreMigrator(fixture.DataSource, options).MigrateAsync(CancellationToken.None);
        await using (var create = fixture.DataSource.CreateCommand(
                         $"CREATE TABLE {schema}.read_model (position bigint PRIMARY KEY);"))
        {
            await create.ExecuteNonQueryAsync(CancellationToken.None);
        }

        var checkpoints = new PostgreSqlProjectionCheckpointStore(fixture.DataSource, options);
        var key = new ProjectionKey("atomic", 1);
        await checkpoints.ProcessAsync(key, CreateCommittedEnvelope(0), InsertReadModel, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => checkpoints.ProcessAsync(
            key, CreateCommittedEnvelope(1), async (context, token) =>
            {
                await InsertReadModel(context, token);
                throw new InvalidOperationException("rollback projection");
            }, CancellationToken.None));

        await using var count = fixture.DataSource.CreateCommand($"SELECT count(*) FROM {schema}.read_model;");
        Assert.Equal(1L, await count.ExecuteScalarAsync(CancellationToken.None));
        Assert.Equal(0, (await checkpoints.GetAsync(key, CancellationToken.None)).LastProcessedPosition);

        async Task InsertReadModel(ProjectionTransactionContext context, CancellationToken token)
        {
            await using var command = new NpgsqlCommand(
                $"INSERT INTO {schema}.read_model (position) VALUES ((SELECT count(*) FROM {schema}.read_model));",
                (NpgsqlConnection)context.Connection!,
                (NpgsqlTransaction)context.Transaction!);
            await command.ExecuteNonQueryAsync(token);
        }
    }

    private async Task<PostgreSqlEventStoreProvider> CreateProviderAsync(string prefix, bool exactSchema = false)
    {
        var schema = exactSchema ? prefix : $"{prefix}_{Guid.NewGuid():N}";
        var options = new PostgreSqlEventStoreOptions { Schema = schema };
        await new PostgreSqlEventStoreMigrator(fixture.DataSource, options).MigrateAsync(CancellationToken.None);
        return new PostgreSqlEventStoreProvider(fixture.DataSource, options);
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
