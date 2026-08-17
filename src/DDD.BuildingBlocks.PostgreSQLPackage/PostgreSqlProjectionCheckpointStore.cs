using System;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Projection;
using Npgsql;

namespace DDD.BuildingBlocks.PostgreSQLPackage;

public sealed class PostgreSqlProjectionCheckpointStore(
    NpgsqlDataSource dataSource,
    PostgreSqlEventStoreOptions? options = null) : IProjectionCheckpointStore
{
    private readonly PostgreSqlEventStoreOptions _options = options ?? new PostgreSqlEventStoreOptions();

    public async Task<ProjectionCheckpoint> GetAsync(ProjectionKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        var sql = $$"""
            SELECT last_processed_position, status, failed_position, last_error, updated_at
            FROM {{_options.Schema}}.projection_checkpoints
            WHERE projection_name = $1 AND projection_version = $2;
            """;
        await using var command = dataSource.CreateCommand(sql);
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

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
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
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
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
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await LockAsync(connection, transaction, key, cancellationToken).ConfigureAwait(false);
        await resetReadModel(new ProjectionTransactionContext(connection, transaction), cancellationToken)
            .ConfigureAwait(false);
        await UpdateAsync(
            connection, transaction, key, -1, ProjectionStatus.Rebuilding, null, null, cancellationToken)
            .ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProjectionCheckpoint> LockAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ProjectionKey key,
        CancellationToken cancellationToken)
    {
        var insert = $$"""
            INSERT INTO {{_options.Schema}}.projection_checkpoints
                (projection_name, projection_version, last_processed_position, status)
            VALUES ($1, $2, -1, $3)
            ON CONFLICT (projection_name, projection_version) DO NOTHING;
            """;
        await using (var command = new NpgsqlCommand(insert, connection, transaction))
        {
            AddKey(command, key);
            command.Parameters.AddWithValue((int)ProjectionStatus.Idle);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var select = $$"""
            SELECT last_processed_position, status, failed_position, last_error, updated_at
            FROM {{_options.Schema}}.projection_checkpoints
            WHERE projection_name = $1 AND projection_version = $2
            FOR UPDATE;
            """;
        await using var selectCommand = new NpgsqlCommand(select, connection, transaction);
        AddKey(selectCommand, key);
        await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        return ReadCheckpoint(reader, key);
    }

    private async Task UpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ProjectionKey key,
        long position,
        ProjectionStatus status,
        long? failedPosition,
        string? error,
        CancellationToken cancellationToken)
    {
        var sql = $$"""
            UPDATE {{_options.Schema}}.projection_checkpoints
            SET last_processed_position = $3, status = $4, failed_position = $5,
                last_error = $6, updated_at = transaction_timestamp()
            WHERE projection_name = $1 AND projection_version = $2;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        AddKey(command, key);
        command.Parameters.AddWithValue(position);
        command.Parameters.AddWithValue((int)status);
        command.Parameters.AddWithValue((object?)failedPosition ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)error ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddKey(NpgsqlCommand command, ProjectionKey key)
    {
        command.Parameters.AddWithValue(key.Name);
        command.Parameters.AddWithValue(key.Version);
    }

    private static ProjectionCheckpoint ReadCheckpoint(NpgsqlDataReader reader, ProjectionKey key) => new(
        key,
        reader.GetInt64(0),
        (ProjectionStatus)reader.GetInt32(1),
        reader.IsDBNull(2) ? null : reader.GetInt64(2),
        reader.IsDBNull(3) ? null : reader.GetString(3),
        new DateTimeOffset(reader.GetFieldValue<DateTime>(4), TimeSpan.Zero));

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
