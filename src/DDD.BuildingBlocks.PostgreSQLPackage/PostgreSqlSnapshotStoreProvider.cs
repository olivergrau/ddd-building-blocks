using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;
using DDD.BuildingBlocks.Core.Persistence.Storage;
using Npgsql;
using NpgsqlTypes;

namespace DDD.BuildingBlocks.PostgreSQLPackage;

public sealed class PostgreSqlSnapshotStoreProvider : ISnapshotStoreProvider
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly PostgreSqlEventStoreOptions _options;

    public PostgreSqlSnapshotStoreProvider(
        NpgsqlDataSource dataSource,
        int snapshotFrequency,
        PostgreSqlEventStoreOptions? options = null)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        ArgumentOutOfRangeException.ThrowIfLessThan(snapshotFrequency, 1);
        SnapshotFrequency = snapshotFrequency;
        _options = options ?? new PostgreSqlEventStoreOptions();
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
            SELECT stream_version, snapshot_type, schema_version, created_at, payload::text
            FROM {{_options.Schema}}.snapshots
            WHERE aggregate_type = $1 AND stream_id = $2 AND ($3::bigint IS NULL OR stream_version <= $3)
            ORDER BY stream_version DESC
            LIMIT 1;
            """;
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue(aggregateType);
        command.Parameters.AddWithValue(streamId);
        command.Parameters.AddWithValue(NpgsqlDbType.Bigint, (object?)maxStreamVersion ?? DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        using var payload = JsonDocument.Parse(reader.GetString(4));
        return new SnapshotEnvelope(
            streamId, aggregateType, reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2),
            new DateTimeOffset(reader.GetFieldValue<DateTime>(3), TimeSpan.Zero), payload.RootElement);
    }

    public async Task WriteAsync(SnapshotEnvelope snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var sql = $$"""
            INSERT INTO {{_options.Schema}}.snapshots
                (aggregate_type, stream_id, stream_version, snapshot_type, schema_version, created_at, payload)
            VALUES ($1, $2, $3, $4, $5, $6, $7)
            ON CONFLICT (aggregate_type, stream_id, stream_version) DO UPDATE
            SET snapshot_type = excluded.snapshot_type, schema_version = excluded.schema_version,
                created_at = excluded.created_at, payload = excluded.payload;
            """;
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue(snapshot.AggregateType);
        command.Parameters.AddWithValue(snapshot.StreamId);
        command.Parameters.AddWithValue(snapshot.StreamVersion);
        command.Parameters.AddWithValue(snapshot.SnapshotType);
        command.Parameters.AddWithValue(snapshot.SchemaVersion);
        command.Parameters.AddWithValue(snapshot.CreatedAt.UtcDateTime);
        command.Parameters.AddWithValue(NpgsqlDbType.Jsonb, snapshot.Payload.GetRawText());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
