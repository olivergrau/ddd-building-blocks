using System;
using System.Data;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;
using DDD.BuildingBlocks.Core.Persistence.Storage;
using Microsoft.Data.SqlClient;

namespace DDD.BuildingBlocks.MSSQLPackage;

public sealed class SqlServerSnapshotStoreProvider : ISnapshotStoreProvider
{
    private readonly string _connectionString;
    private readonly SqlServerEventStoreOptions _options;

    public SqlServerSnapshotStoreProvider(
        string connectionString,
        int snapshotFrequency,
        SqlServerEventStoreOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentOutOfRangeException.ThrowIfLessThan(snapshotFrequency, 1);
        _connectionString = connectionString;
        SnapshotFrequency = snapshotFrequency;
        _options = options ?? new SqlServerEventStoreOptions();
    }

    public int SnapshotFrequency { get; }

    public async Task<SnapshotEnvelope?> ReadAsync(
        string streamId,
        string aggregateType,
        long? maxStreamVersion,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        if (maxStreamVersion is < 0) throw new ArgumentOutOfRangeException(nameof(maxStreamVersion));
        var sql = $$"""
            SELECT TOP (1) [StreamVersion], [SnapshotType], [SchemaVersion], [CreatedAt], [Payload]
            FROM [{{_options.Schema}}].[Snapshots]
            WHERE [AggregateType] = @AggregateType AND [StreamId] = @StreamId
              AND (@MaxStreamVersion IS NULL OR [StreamVersion] <= @MaxStreamVersion)
            ORDER BY [StreamVersion] DESC;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        AddIdentity(command, streamId, aggregateType);
        command.Parameters.Add("@MaxStreamVersion", SqlDbType.BigInt).Value = (object?)maxStreamVersion ?? DBNull.Value;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        using var payload = JsonDocument.Parse(reader.GetString(4));
        return new SnapshotEnvelope(
            streamId, aggregateType, reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2),
            reader.GetFieldValue<DateTimeOffset>(3), payload.RootElement);
    }

    public async Task WriteAsync(SnapshotEnvelope snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var sql = $$"""
            UPDATE [{{_options.Schema}}].[Snapshots] WITH (UPDLOCK, HOLDLOCK)
            SET [SnapshotType] = @SnapshotType, [SchemaVersion] = @SchemaVersion,
                [CreatedAt] = @CreatedAt, [Payload] = @Payload
            WHERE [AggregateType] = @AggregateType AND [StreamId] = @StreamId AND [StreamVersion] = @StreamVersion;
            IF @@ROWCOUNT = 0
                INSERT INTO [{{_options.Schema}}].[Snapshots]
                    ([AggregateType], [StreamId], [StreamVersion], [SnapshotType], [SchemaVersion], [CreatedAt], [Payload])
                VALUES (@AggregateType, @StreamId, @StreamVersion, @SnapshotType, @SchemaVersion, @CreatedAt, @Payload);
            """;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection, (SqlTransaction)transaction);
        AddIdentity(command, snapshot.StreamId, snapshot.AggregateType);
        command.Parameters.Add("@StreamVersion", SqlDbType.BigInt).Value = snapshot.StreamVersion;
        command.Parameters.Add("@SnapshotType", SqlDbType.NVarChar, 512).Value = snapshot.SnapshotType;
        command.Parameters.Add("@SchemaVersion", SqlDbType.Int).Value = snapshot.SchemaVersion;
        command.Parameters.Add("@CreatedAt", SqlDbType.DateTimeOffset).Value = snapshot.CreatedAt;
        command.Parameters.Add("@Payload", SqlDbType.NVarChar, -1).Value = snapshot.Payload.GetRawText();
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddIdentity(SqlCommand command, string streamId, string aggregateType)
    {
        command.Parameters.Add("@AggregateType", SqlDbType.NVarChar, 512).Value = aggregateType;
        command.Parameters.Add("@StreamId", SqlDbType.NVarChar, 512).Value = streamId;
    }
}
