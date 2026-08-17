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
using Microsoft.Data.SqlClient;

namespace DDD.BuildingBlocks.MSSQLPackage;

public sealed class SqlServerEventStoreProvider(
    string connectionString,
    SqlServerEventStoreOptions? options = null) : IEventStoreProvider
{
    private readonly SqlServerEventStoreOptions _options = options ?? new SqlServerEventStoreOptions();

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
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = (SqlTransaction)await connection
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
                .ConfigureAwait(false);

            var actualVersion = await LockStreamAsync(
                connection, transaction, streamId, aggregateType, cancellationToken).ConfigureAwait(false);
            if (actualVersion is null && expectedVersion == -1)
            {
                await CreateStreamAsync(connection, transaction, streamId, aggregateType, cancellationToken)
                    .ConfigureAwait(false);
                actualVersion = -1;
            }

            ThrowOnVersionMismatch(streamId, expectedVersion, actualVersion);
            var firstPosition = await ReserveGlobalPositionsAsync(
                connection, transaction, events.Count, cancellationToken).ConfigureAwait(false);
            await InsertEventsAsync(connection, transaction, events, firstPosition, cancellationToken).ConfigureAwait(false);

            var currentVersion = expectedVersion + events.Count;
            await UpdateStreamVersionAsync(
                connection, transaction, streamId, aggregateType, currentVersion, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return new AppendEventsResult(currentVersion, firstPosition, firstPosition + events.Count - 1L);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException exception) when (IsDuplicateEventId(exception))
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
        catch (SqlException exception)
        {
            throw new EventStoreProviderException(
                "SQL Server event-store operation failed.", IsTransient(exception), exception);
        }
    }

    public Task<IReadOnlyList<EventEnvelope>> ReadStreamAsync(
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
            SELECT TOP (@MaxCount)
                [EventId], [StreamId], [AggregateType], [StreamVersion], [GlobalPosition],
                [EventType], [SchemaVersion], [OccurredAt], [CommittedAt], [CorrelationId],
                [CausationId], [CommandId], [Actor], [TurnId], [Payload]
            FROM [{{_options.Schema}}].[Events]
            WHERE [AggregateType] = @AggregateType
              AND [StreamId] = @StreamId
              AND [StreamVersion] >= @FromStreamVersion
            ORDER BY [StreamVersion];
            """;

        return ReadAsync(
            sql,
            command =>
            {
                command.Parameters.Add("@MaxCount", SqlDbType.Int).Value = maxCount;
                command.Parameters.Add("@AggregateType", SqlDbType.NVarChar, 512).Value = aggregateType;
                command.Parameters.Add("@StreamId", SqlDbType.NVarChar, 512).Value = streamId;
                command.Parameters.Add("@FromStreamVersion", SqlDbType.BigInt).Value = fromStreamVersion;
            },
            cancellationToken);
    }

    public Task<IReadOnlyList<EventEnvelope>> ReadCommittedFeedAsync(
        long afterGlobalPosition,
        int maxCount,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(afterGlobalPosition, -1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, 1);

        var sql = $$"""
            SELECT TOP (@MaxCount)
                [EventId], [StreamId], [AggregateType], [StreamVersion], [GlobalPosition],
                [EventType], [SchemaVersion], [OccurredAt], [CommittedAt], [CorrelationId],
                [CausationId], [CommandId], [Actor], [TurnId], [Payload]
            FROM [{{_options.Schema}}].[Events]
            WHERE [GlobalPosition] > @AfterGlobalPosition
            ORDER BY [GlobalPosition];
            """;

        return ReadAsync(
            sql,
            command =>
            {
                command.Parameters.Add("@MaxCount", SqlDbType.Int).Value = maxCount;
                command.Parameters.Add("@AfterGlobalPosition", SqlDbType.BigInt).Value = afterGlobalPosition;
            },
            cancellationToken);
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

    private async Task<long?> LockStreamAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string streamId,
        string aggregateType,
        CancellationToken cancellationToken)
    {
        var sql = $$"""
            SELECT [CurrentVersion]
            FROM [{{_options.Schema}}].[EventStreams] WITH (UPDLOCK, HOLDLOCK)
            WHERE [AggregateType] = @AggregateType AND [StreamId] = @StreamId;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        AddStreamParameters(command, streamId, aggregateType);
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return value is null ? null : (long)value;
    }

    private async Task CreateStreamAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string streamId,
        string aggregateType,
        CancellationToken cancellationToken)
    {
        var sql = $$"""
            INSERT INTO [{{_options.Schema}}].[EventStreams] ([AggregateType], [StreamId], [CurrentVersion])
            VALUES (@AggregateType, @StreamId, -1);
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        AddStreamParameters(command, streamId, aggregateType);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void ThrowOnVersionMismatch(string streamId, long expectedVersion, long? actualVersion)
    {
        if (actualVersion == expectedVersion)
        {
            return;
        }

        if (expectedVersion == -1 && actualVersion is not null)
        {
            throw new EventStreamAlreadyExistsException(streamId, actualVersion.Value);
        }

        if (actualVersion is null)
        {
            throw new EventStreamNotFoundException(streamId);
        }

        throw new EventStoreConcurrencyException(streamId, expectedVersion, actualVersion.Value);
    }

    private async Task<long> ReserveGlobalPositionsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int count,
        CancellationToken cancellationToken)
    {
        var sql = $$"""
            UPDATE [{{_options.Schema}}].[EventStoreState] WITH (UPDLOCK)
            SET [NextGlobalPosition] = [NextGlobalPosition] + @Count
            OUTPUT deleted.[NextGlobalPosition]
            WHERE [Singleton] = 1;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Count", SqlDbType.Int).Value = count;
        return (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    private async Task InsertEventsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        IReadOnlyCollection<EventEnvelope> events,
        long firstGlobalPosition,
        CancellationToken cancellationToken)
    {
        var sql = $$"""
            INSERT INTO [{{_options.Schema}}].[Events]
                ([GlobalPosition], [EventId], [AggregateType], [StreamId], [StreamVersion], [EventType],
                 [SchemaVersion], [OccurredAt], [CorrelationId], [CausationId], [CommandId], [Actor], [TurnId], [Payload])
            SELECT @FirstGlobalPosition + [BatchIndex], [EventId], [AggregateType], [StreamId], [StreamVersion],
                   [EventType], [SchemaVersion], [OccurredAt], [CorrelationId], [CausationId], [CommandId],
                   [Actor], [TurnId], [Payload]
            FROM @Events;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@FirstGlobalPosition", SqlDbType.BigInt).Value = firstGlobalPosition;
        command.Parameters.Add(new SqlParameter("@Events", SqlDbType.Structured)
        {
            TypeName = $"{_options.Schema}.EventBatchType",
            Value = CreateEventBatch(events),
        });
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static DataTable CreateEventBatch(IReadOnlyCollection<EventEnvelope> events)
    {
        var table = new DataTable { Locale = System.Globalization.CultureInfo.InvariantCulture };
        table.Columns.Add("BatchIndex", typeof(int));
        table.Columns.Add("EventId", typeof(Guid));
        table.Columns.Add("AggregateType", typeof(string));
        table.Columns.Add("StreamId", typeof(string));
        table.Columns.Add("StreamVersion", typeof(long));
        table.Columns.Add("EventType", typeof(string));
        table.Columns.Add("SchemaVersion", typeof(int));
        table.Columns.Add("OccurredAt", typeof(DateTimeOffset));
        table.Columns.Add("CorrelationId", typeof(string));
        table.Columns.Add("CausationId", typeof(string));
        table.Columns.Add("CommandId", typeof(string));
        table.Columns.Add("Actor", typeof(string));
        table.Columns.Add("TurnId", typeof(string));
        table.Columns.Add("Payload", typeof(string));

        var index = 0;
        foreach (var envelope in events)
        {
            table.Rows.Add(
                index++, envelope.EventId, envelope.AggregateType, envelope.StreamId, envelope.StreamVersion,
                envelope.EventType, envelope.SchemaVersion, envelope.OccurredAt,
                DbValue(envelope.CorrelationId), DbValue(envelope.CausationId), DbValue(envelope.CommandId),
                DbValue(envelope.Actor), DbValue(envelope.TurnId), envelope.Payload.GetRawText());
        }

        return table;
    }

    private async Task UpdateStreamVersionAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string streamId,
        string aggregateType,
        long currentVersion,
        CancellationToken cancellationToken)
    {
        var sql = $$"""
            UPDATE [{{_options.Schema}}].[EventStreams]
            SET [CurrentVersion] = @CurrentVersion, [UpdatedAt] = SYSDATETIMEOFFSET()
            WHERE [AggregateType] = @AggregateType AND [StreamId] = @StreamId;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@CurrentVersion", SqlDbType.BigInt).Value = currentVersion;
        AddStreamParameters(command, streamId, aggregateType);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<EventEnvelope>> ReadAsync(
        string sql,
        Action<SqlCommand> addParameters,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new SqlCommand(sql, connection);
            addParameters(command);
            var result = new List<EventEnvelope>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                using var payload = JsonDocument.Parse(reader.GetString(14));
                result.Add(new EventEnvelope(
                    reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4),
                    reader.GetString(5), reader.GetInt32(6), reader.GetFieldValue<DateTimeOffset>(7),
                    reader.GetFieldValue<DateTimeOffset>(8), ReadNullableString(reader, 9), ReadNullableString(reader, 10),
                    ReadNullableString(reader, 11), ReadNullableString(reader, 12), ReadNullableString(reader, 13),
                    payload.RootElement));
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException exception)
        {
            throw new EventStoreProviderException("SQL Server event-store read failed.", IsTransient(exception), exception);
        }
    }

    private async Task<Guid> FindCommittedEventIdAsync(Guid[] candidates)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(CancellationToken.None).ConfigureAwait(false);
        var values = string.Join(",", candidates.Select((_, index) => $"@EventId{index}"));
        await using var command = new SqlCommand($$"""
            SELECT TOP (1) [EventId]
            FROM [{{_options.Schema}}].[Events]
            WHERE [EventId] IN ({{values}});
            """, connection);
        for (var index = 0; index < candidates.Length; index++)
        {
            command.Parameters.Add($"@EventId{index}", SqlDbType.UniqueIdentifier).Value = candidates[index];
        }

        var result = await command.ExecuteScalarAsync(CancellationToken.None).ConfigureAwait(false);
        return result is Guid eventId ? eventId : candidates[0];
    }

    private static void AddStreamParameters(SqlCommand command, string streamId, string aggregateType)
    {
        command.Parameters.Add("@AggregateType", SqlDbType.NVarChar, 512).Value = aggregateType;
        command.Parameters.Add("@StreamId", SqlDbType.NVarChar, 512).Value = streamId;
    }

    private static object DbValue(string? value) => value is null ? DBNull.Value : value;

    private static string? ReadNullableString(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static bool IsDuplicateEventId(SqlException exception) =>
        (exception.Number is 2601 or 2627) &&
        exception.Message.Contains("UQ_Events_EventId", StringComparison.Ordinal);

    private static bool IsTransient(SqlException exception) => exception.Number is
        -2 or 1205 or 4060 or 10928 or 10929 or 40197 or 40501 or 40613 or 49918 or 49919 or 49920 or
        10053 or 10054 or 10060;
}
