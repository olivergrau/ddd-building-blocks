using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Projection;
using Microsoft.Data.SqlClient;

namespace DDD.BuildingBlocks.MSSQLPackage;

public sealed class SqlServerProjectionCheckpointStore(
    string connectionString,
    SqlServerEventStoreOptions? options = null) : IProjectionCheckpointStore
{
    private readonly SqlServerEventStoreOptions _options = options ?? new SqlServerEventStoreOptions();

    public async Task<ProjectionCheckpoint> GetAsync(ProjectionKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        var sql = $$"""
            SELECT [LastProcessedPosition], [Status], [FailedPosition], [LastError], [UpdatedAt]
            FROM [{{_options.Schema}}].[ProjectionCheckpoints]
            WHERE [ProjectionName] = @ProjectionName AND [ProjectionVersion] = @ProjectionVersion;
            """;
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        AddKey(command, key);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? ReadCheckpoint(reader, key)
            : Initial(key);
    }

    public async Task<bool> ProcessAsync(
        ProjectionKey key,
        EventEnvelope envelope,
        Func<ProjectionTransactionContext, CancellationToken, Task> apply,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(apply);
        var position = envelope.GlobalPosition ?? throw new ArgumentException(
            "A projection requires a committed envelope.", nameof(envelope));

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var checkpoint = await LockAsync(connection, transaction, key, cancellationToken).ConfigureAwait(false);
        if (position <= checkpoint.LastProcessedPosition)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        EnsureNext(key, checkpoint.LastProcessedPosition, position);
        await apply(new ProjectionTransactionContext(connection, transaction), cancellationToken).ConfigureAwait(false);
        await UpdateAsync(
            connection, transaction, key, position, ProjectionStatus.Running, null, null, cancellationToken)
            .ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task MarkFailedAsync(
        ProjectionKey key,
        long failedPosition,
        string error,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var checkpoint = await LockAsync(connection, transaction, key, cancellationToken).ConfigureAwait(false);
        await UpdateAsync(
            connection, transaction, key, checkpoint.LastProcessedPosition, ProjectionStatus.Faulted,
            failedPosition, error, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ResetAsync(
        ProjectionKey key,
        Func<ProjectionTransactionContext, CancellationToken, Task> resetReadModel,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(resetReadModel);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await LockAsync(connection, transaction, key, cancellationToken).ConfigureAwait(false);
        await resetReadModel(new ProjectionTransactionContext(connection, transaction), cancellationToken)
            .ConfigureAwait(false);
        await UpdateAsync(
            connection, transaction, key, -1, ProjectionStatus.Rebuilding, null, null, cancellationToken)
            .ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProjectionCheckpoint> LockAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        ProjectionKey key,
        CancellationToken cancellationToken)
    {
        var select = $$"""
            SELECT [LastProcessedPosition], [Status], [FailedPosition], [LastError], [UpdatedAt]
            FROM [{{_options.Schema}}].[ProjectionCheckpoints] WITH (UPDLOCK, HOLDLOCK)
            WHERE [ProjectionName] = @ProjectionName AND [ProjectionVersion] = @ProjectionVersion;
            """;
        await using (var command = new SqlCommand(select, connection, transaction))
        {
            AddKey(command, key);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return ReadCheckpoint(reader, key);
            }
        }

        var insert = $$"""
            INSERT INTO [{{_options.Schema}}].[ProjectionCheckpoints]
                ([ProjectionName], [ProjectionVersion], [LastProcessedPosition], [Status])
            VALUES (@ProjectionName, @ProjectionVersion, -1, @Status);
            """;
        await using var insertCommand = new SqlCommand(insert, connection, transaction);
        AddKey(insertCommand, key);
        insertCommand.Parameters.Add("@Status", SqlDbType.Int).Value = (int)ProjectionStatus.Idle;
        await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return Initial(key);
    }

    private async Task UpdateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        ProjectionKey key,
        long position,
        ProjectionStatus status,
        long? failedPosition,
        string? error,
        CancellationToken cancellationToken)
    {
        var sql = $$"""
            UPDATE [{{_options.Schema}}].[ProjectionCheckpoints]
            SET [LastProcessedPosition] = @Position, [Status] = @Status,
                [FailedPosition] = @FailedPosition, [LastError] = @LastError,
                [UpdatedAt] = SYSDATETIMEOFFSET()
            WHERE [ProjectionName] = @ProjectionName AND [ProjectionVersion] = @ProjectionVersion;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        AddKey(command, key);
        command.Parameters.Add("@Position", SqlDbType.BigInt).Value = position;
        command.Parameters.Add("@Status", SqlDbType.Int).Value = (int)status;
        command.Parameters.Add("@FailedPosition", SqlDbType.BigInt).Value = (object?)failedPosition ?? DBNull.Value;
        command.Parameters.Add("@LastError", SqlDbType.NVarChar, -1).Value = (object?)error ?? DBNull.Value;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddKey(SqlCommand command, ProjectionKey key)
    {
        command.Parameters.Add("@ProjectionName", SqlDbType.NVarChar, 512).Value = key.Name;
        command.Parameters.Add("@ProjectionVersion", SqlDbType.Int).Value = key.Version;
    }

    private static ProjectionCheckpoint ReadCheckpoint(SqlDataReader reader, ProjectionKey key) => new(
        key,
        reader.GetInt64(0),
        (ProjectionStatus)reader.GetInt32(1),
        reader.IsDBNull(2) ? null : reader.GetInt64(2),
        reader.IsDBNull(3) ? null : reader.GetString(3),
        reader.GetFieldValue<DateTimeOffset>(4));

    private static ProjectionCheckpoint Initial(ProjectionKey key) =>
        new(key, -1, ProjectionStatus.Idle, null, null, DateTimeOffset.UtcNow);

    private static void EnsureNext(ProjectionKey key, long lastPosition, long position)
    {
        if (position != lastPosition + 1)
        {
            throw new InvalidOperationException(
                $"Projection '{key.Name}' expected position {lastPosition + 1}, but received {position}.");
        }
    }
}
