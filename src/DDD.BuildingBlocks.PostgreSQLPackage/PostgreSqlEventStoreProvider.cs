using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Persistence.Storage;
using Npgsql;

namespace DDD.BuildingBlocks.PostgreSQLPackage;

public sealed class PostgreSqlEventStoreProvider(
    NpgsqlDataSource dataSource,
    PostgreSqlEventStoreOptions? options = null) : IEventStoreProvider
{
    private readonly PostgreSqlEventStoreOptions _options = options ?? new PostgreSqlEventStoreOptions();

    public async Task<AppendEventsResult> AppendAsync(
        string streamId,
        string aggregateType,
        long expectedVersion,
        IReadOnlyCollection<EventEnvelope> events,
        CancellationToken cancellationToken)
    {
        ValidateAppend(streamId, aggregateType, expectedVersion, events);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
                .ConfigureAwait(false);

            var actualVersion = expectedVersion == -1
                ? await CreateStreamAsync(connection, transaction, streamId, aggregateType, cancellationToken).ConfigureAwait(false)
                : await LockStreamAsync(connection, transaction, streamId, aggregateType, cancellationToken).ConfigureAwait(false);

            ThrowOnVersionMismatch(streamId, expectedVersion, actualVersion);

            var firstPosition = await ReserveGlobalPositionsAsync(
                connection, transaction, events.Count, cancellationToken).ConfigureAwait(false);
            await InsertEventsAsync(
                connection, transaction, events, firstPosition, cancellationToken).ConfigureAwait(false);
            var lastPosition = firstPosition + events.Count - 1L;

            var currentVersion = expectedVersion + events.Count;
            await UpdateStreamVersionAsync(
                connection, transaction, streamId, aggregateType, currentVersion, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return new AppendEventsResult(currentVersion, firstPosition, lastPosition);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation &&
                                                  exception.ConstraintName == "events_event_id_key")
        {
            var duplicate = await FindCommittedEventIdAsync(events.Select(item => item.EventId).ToArray()).ConfigureAwait(false);
            throw new DuplicateEventIdException(duplicate);
        }
        catch (EventStreamAlreadyExistsException)
        {
            throw;
        }
        catch (EventStreamNotFoundException)
        {
            throw;
        }
        catch (EventStoreConcurrencyException)
        {
            throw;
        }
        catch (NpgsqlException exception)
        {
            throw new EventStoreProviderException("PostgreSQL event-store operation failed.", exception.IsTransient, exception);
        }
    }

    public async Task<IReadOnlyList<EventEnvelope>> ReadStreamAsync(
        string streamId,
        string aggregateType,
        long fromStreamVersion,
        int maxCount,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentOutOfRangeException.ThrowIfNegative(fromStreamVersion);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, 1);

        var sql = $$"""
            SELECT event_id, stream_id, aggregate_type, stream_version, global_position,
                   event_type, schema_version, occurred_at, committed_at, correlation_id,
                   causation_id, command_id, actor, turn_id, payload::text
            FROM {{_options.Schema}}.events
            WHERE aggregate_type = $1 AND stream_id = $2 AND stream_version >= $3
            ORDER BY stream_version
            LIMIT $4;
            """;

        return await ReadAsync(
            sql,
            command =>
            {
                command.Parameters.AddWithValue(aggregateType);
                command.Parameters.AddWithValue(streamId);
                command.Parameters.AddWithValue(fromStreamVersion);
                command.Parameters.AddWithValue(maxCount);
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<EventEnvelope>> ReadCommittedFeedAsync(
        long afterGlobalPosition,
        int maxCount,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(afterGlobalPosition, -1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, 1);

        var sql = $$"""
            SELECT event_id, stream_id, aggregate_type, stream_version, global_position,
                   event_type, schema_version, occurred_at, committed_at, correlation_id,
                   causation_id, command_id, actor, turn_id, payload::text
            FROM {{_options.Schema}}.events
            WHERE global_position > $1
            ORDER BY global_position
            LIMIT $2;
            """;

        return await ReadAsync(
            sql,
            command =>
            {
                command.Parameters.AddWithValue(afterGlobalPosition);
                command.Parameters.AddWithValue(maxCount);
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateAppend(
        string streamId,
        string aggregateType,
        long expectedVersion,
        IReadOnlyCollection<EventEnvelope> events)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentOutOfRangeException.ThrowIfLessThan(expectedVersion, -1);
        if (events.Count == 0)
        {
            throw new ArgumentException("At least one event is required.", nameof(events));
        }

        var eventIds = new HashSet<Guid>();
        var nextVersion = expectedVersion + 1;
        foreach (var envelope in events)
        {
            if (!string.Equals(envelope.StreamId, streamId, StringComparison.Ordinal) ||
                !string.Equals(envelope.AggregateType, aggregateType, StringComparison.Ordinal))
            {
                throw new ArgumentException("Every envelope must target the appended stream and aggregate type.", nameof(events));
            }

            if (envelope.StreamVersion != nextVersion++)
            {
                throw new ArgumentException("Envelope stream versions must be contiguous after the expected version.", nameof(events));
            }

            if (envelope.GlobalPosition is not null || envelope.CommittedAt is not null)
            {
                throw new ArgumentException("Global position and commit timestamp are assigned by the provider.", nameof(events));
            }

            if (!eventIds.Add(envelope.EventId))
            {
                throw new DuplicateEventIdException(envelope.EventId);
            }
        }
    }

    private async Task<long> CreateStreamAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string streamId,
        string aggregateType,
        CancellationToken cancellationToken)
    {
        var sql = $$"""
            INSERT INTO {{_options.Schema}}.event_streams (aggregate_type, stream_id, current_version)
            VALUES ($1, $2, -1)
            ON CONFLICT (aggregate_type, stream_id) DO NOTHING
            RETURNING current_version;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue(aggregateType);
        command.Parameters.AddWithValue(streamId);
        var inserted = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return inserted is not null
            ? -1
            : await LockStreamAsync(connection, transaction, streamId, aggregateType, cancellationToken).ConfigureAwait(false);
    }

    private async Task<long> LockStreamAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string streamId,
        string aggregateType,
        CancellationToken cancellationToken)
    {
        var sql = $$"""
            SELECT current_version
            FROM {{_options.Schema}}.event_streams
            WHERE aggregate_type = $1 AND stream_id = $2
            FOR UPDATE;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue(aggregateType);
        command.Parameters.AddWithValue(streamId);
        var version = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return version is null ? -1 : (long)version;
    }

    private static void ThrowOnVersionMismatch(string streamId, long expectedVersion, long actualVersion)
    {
        if (actualVersion == expectedVersion)
        {
            return;
        }

        if (expectedVersion == -1)
        {
            throw new EventStreamAlreadyExistsException(streamId, actualVersion);
        }

        if (actualVersion == -1)
        {
            throw new EventStreamNotFoundException(streamId);
        }

        throw new EventStoreConcurrencyException(streamId, expectedVersion, actualVersion);
    }

    private async Task<long> ReserveGlobalPositionsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int count,
        CancellationToken cancellationToken)
    {
        var sql = $$"""
            UPDATE {{_options.Schema}}.event_store_state
            SET next_global_position = next_global_position + $1
            WHERE singleton = TRUE
            RETURNING next_global_position - $1;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue(count);
        var first = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return (long)first!;
    }

    private async Task InsertEventsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyCollection<EventEnvelope> events,
        long firstGlobalPosition,
        CancellationToken cancellationToken)
    {
        var sql = $$"""
            INSERT INTO {{_options.Schema}}.events
                (global_position, event_id, aggregate_type, stream_id, stream_version, event_type, schema_version,
                 occurred_at, correlation_id, causation_id, command_id, actor, turn_id, payload)
            SELECT global_position, event_id, aggregate_type, stream_id, stream_version, event_type, schema_version,
                   occurred_at, correlation_id, causation_id, command_id, actor, turn_id, payload_text::jsonb
            FROM unnest(
                $1::bigint[], $2::uuid[], $3::text[], $4::text[], $5::bigint[], $6::text[], $7::integer[],
                $8::timestamp with time zone[], $9::text[], $10::text[], $11::text[], $12::text[], $13::text[], $14::text[])
            AS batch(global_position, event_id, aggregate_type, stream_id, stream_version, event_type, schema_version,
                     occurred_at, correlation_id, causation_id, command_id, actor, turn_id, payload_text);
            """;
        var batch = events.ToArray();
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue(Enumerable.Range(0, batch.Length).Select(i => firstGlobalPosition + i).ToArray());
        command.Parameters.AddWithValue(batch.Select(item => item.EventId).ToArray());
        command.Parameters.AddWithValue(batch.Select(item => item.AggregateType).ToArray());
        command.Parameters.AddWithValue(batch.Select(item => item.StreamId).ToArray());
        command.Parameters.AddWithValue(batch.Select(item => item.StreamVersion).ToArray());
        command.Parameters.AddWithValue(batch.Select(item => item.EventType).ToArray());
        command.Parameters.AddWithValue(batch.Select(item => item.SchemaVersion).ToArray());
        command.Parameters.AddWithValue(batch.Select(item => item.OccurredAt.UtcDateTime).ToArray());
        command.Parameters.AddWithValue(batch.Select(item => item.CorrelationId).ToArray());
        command.Parameters.AddWithValue(batch.Select(item => item.CausationId).ToArray());
        command.Parameters.AddWithValue(batch.Select(item => item.CommandId).ToArray());
        command.Parameters.AddWithValue(batch.Select(item => item.Actor).ToArray());
        command.Parameters.AddWithValue(batch.Select(item => item.TurnId).ToArray());
        command.Parameters.AddWithValue(batch.Select(item => item.Payload.GetRawText()).ToArray());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task UpdateStreamVersionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string streamId,
        string aggregateType,
        long currentVersion,
        CancellationToken cancellationToken)
    {
        var sql = $$"""
            UPDATE {{_options.Schema}}.event_streams
            SET current_version = $1, updated_at = transaction_timestamp()
            WHERE aggregate_type = $2 AND stream_id = $3;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue(currentVersion);
        command.Parameters.AddWithValue(aggregateType);
        command.Parameters.AddWithValue(streamId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<EventEnvelope>> ReadAsync(
        string sql,
        Action<NpgsqlCommand> addParameters,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = dataSource.CreateCommand(sql);
            addParameters(command);
            var result = new List<EventEnvelope>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                using var payload = JsonDocument.Parse(reader.GetString(14));
                result.Add(new EventEnvelope(
                    reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4),
                    reader.GetString(5), reader.GetInt32(6), ReadTimestamp(reader, 7), ReadTimestamp(reader, 8),
                    ReadNullableString(reader, 9), ReadNullableString(reader, 10), ReadNullableString(reader, 11),
                    ReadNullableString(reader, 12), ReadNullableString(reader, 13), payload.RootElement));
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (NpgsqlException exception)
        {
            throw new EventStoreProviderException("PostgreSQL event-store read failed.", exception.IsTransient, exception);
        }
    }

    private async Task<Guid> FindCommittedEventIdAsync(Guid[] candidates)
    {
        var sql = $$"""
            SELECT event_id
            FROM {{_options.Schema}}.events
            WHERE event_id = ANY($1)
            LIMIT 1;
            """;
        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue(candidates);
        var result = await command.ExecuteScalarAsync(CancellationToken.None).ConfigureAwait(false);
        return result is Guid eventId ? eventId : candidates[0];
    }

    private static DateTimeOffset ReadTimestamp(NpgsqlDataReader reader, int ordinal) =>
        new(reader.GetFieldValue<DateTime>(ordinal), TimeSpan.Zero);

    private static string? ReadNullableString(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
}
