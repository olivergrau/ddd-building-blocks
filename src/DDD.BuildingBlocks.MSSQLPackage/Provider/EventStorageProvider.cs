using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DDD.BuildingBlocks.MSSQLPackage.Provider;

using System.Data;
using System.Globalization;
using Core.Attribute;
using Core.Domain;
using Core.Event;
using Core.Exception;
using Core.Persistence;
using Core.Persistence.Storage;
using Core.Util;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

/// <summary>
///     MSSQL Server specific Storage Provider for an Event Store.
/// </summary>
public sealed class EventStorageProvider : IEventStorageProvider
{
    private readonly IOptionsMonitor<EventStorageProviderSettings> _settings;

    public EventStorageProvider(IOptionsMonitor<EventStorageProviderSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        _settings = settings;
    }

    public async Task<IEnumerable<IDomainEvent>?> GetEventsAsync(Type aggregateType, string key, int start,
        int count)
    {
        await using var connection = new SqlConnection(_settings.CurrentValue.ConnectionString);
        await using var command = connection.CreateCommand();
        var events = new List<IDomainEvent>();

        try
        {
            await connection.OpenAsync();

            command.CommandText =
                "SELECT E.*, M.[KEY] FROM dbo.MAPPINGS M INNER JOIN dbo.EVENTS E ON M.AGGREGATEID = E.AGGREGATEID LEFT JOIN dbo.AGGREGATES A " +
                "ON E.AGGREGATEID = A.AGGREGATEID " +
                "WHERE M.[KEY] = @key AND M.TYPE = '" + aggregateType.AssemblyQualifiedName +
                "' AND E.VERSION BETWEEN @start1 AND @end2 AND A.TYPE = '" + aggregateType.AssemblyQualifiedName + "'" +
                "ORDER BY E.VERSION ASC";

            command.Parameters.Add(key.ToSqlParameter("@key"));
            command.Parameters.Add(start.ToSqlParameter("@start1"));
            command.Parameters.Add(count == int.MaxValue
                ? (int.MaxValue - start).ToSqlParameter("@end2")
                : (start + count - 1).ToSqlParameter("@end2"));

            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                EventStorageProviderHelper.ReconstituteEvent(reader, events);
            }

            await reader.CloseAsync();
            return events;
        }
        catch (Exception ex)
        {
            throw new ProviderException("Failure reading events from storage.", ex);
        }
    }

    public async Task<IDomainEvent?> GetLastEventAsync(Type aggregateType, string key)
    {
        await using var connection = new SqlConnection(_settings.CurrentValue.ConnectionString);
        await using var command = connection.CreateCommand();

        await connection.OpenAsync();
        command.CommandText =
            "SELECT TOP 1 E.*, M.[KEY] FROM dbo.MAPPINGS M INNER JOIN dbo.EVENTS E ON M.AGGREGATEID = E.AGGREGATEID LEFT JOIN dbo.AGGREGATES A ON E.AGGREGATEID = A.AGGREGATEID WHERE M.[KEY] = @id AND M.TYPE = '" +
            aggregateType.AssemblyQualifiedName + "' AND A.TYPE = '" + aggregateType.AssemblyQualifiedName +
            "' ORDER BY E.VERSION DESC";

        command.Parameters.Add(key.ToSqlParameter("@id"));

        var reader = await command.ExecuteReaderAsync();

        var events = new List<IDomainEvent>();
        while (await reader.ReadAsync())
        {
            EventStorageProviderHelper.ReconstituteEvent(reader, events);
        }

        return events.Any() ? events.First() : null;
    }

    public async Task CommitChangesAsync(IEventSourcingBasedAggregate aggregate)
    {
        var events = aggregate.GetUncommittedChanges();

        var domainEvents = events as IDomainEvent[] ?? events.ToArray();
        if (domainEvents.Any())
        {
            var lastCommittedVersion = aggregate.LastCommittedVersion;

            await using var connection = new SqlConnection(_settings.CurrentValue.ConnectionString);
            await using var sqlCommand1 = connection.CreateCommand();
            await connection.OpenAsync();

            await using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            sqlCommand1.Transaction = transaction;

            try
            {
                sqlCommand1.CommandText =
                    "SELECT M.AGGREGATEID, A.VERSION FROM dbo.MAPPINGS M INNER JOIN dbo.AGGREGATES A WITH (UPDLOCK) ON A.AGGREGATEID = M.AGGREGATEID WHERE M.[KEY] = @key AND M.TYPE = '" +
                    aggregate.GetType().AssemblyQualifiedName + "' AND A.TYPE = '" + aggregate.GetType().AssemblyQualifiedName + "'";
                sqlCommand1.Parameters.Add(aggregate.SerializedId.ToSqlParameter("@key"));

                var reader = await sqlCommand1.ExecuteReaderAsync(); // ODP.NET returns integers as decimals...

                Guid physicalId;
                var version = -1;

                if (!await reader.ReadAsync())
                {
                    physicalId = await CreateNewAggregateStreamAsync(aggregate, connection, transaction);
                }
                else
                {
                    physicalId = reader.GetGuid(0);
                    version = reader.GetInt32(1);
                }

                await reader.CloseAsync();

                if (Convert.ToInt32(version) != lastCommittedVersion)
                {
                    throw new ProviderException(
                        $"Concurrency problem with target version: {version} != {lastCommittedVersion}");
                }

                lastCommittedVersion = await AppendNewEventsAsync(domainEvents, connection, transaction, physicalId, lastCommittedVersion);
                await EnsureUniqueConstraintsAsync(physicalId, aggregate, connection, transaction);

                await using var sqlCommand5 = connection.CreateCommand();
                sqlCommand5.Transaction = transaction;

                sqlCommand5.CommandText =
                    "UPDATE dbo.AGGREGATES SET version = @lastVersion WHERE AGGREGATEID = @id";

                sqlCommand5.Parameters.Clear();
                sqlCommand5.Parameters.Add(lastCommittedVersion.ToSqlParameter("@lastVersion"));
                sqlCommand5.Parameters.Add(physicalId.ToSqlParameter("@id"));

                await sqlCommand5.ExecuteNonQueryAsync();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new ProviderException("Failure committing changes to storage.", ex);
            }
        }
    }

    private static async Task<int> AppendNewEventsAsync(IDomainEvent[] domainEvents, SqlConnection connection, SqlTransaction transaction, Guid physicalId,
        int eventCount
    )
    {
        foreach (var @event in domainEvents)
        {
            var eventData = EventStorageProviderHelper.SerializeEvent(@event);

            await using var sqlCommand4 = connection.CreateCommand();
            sqlCommand4.Transaction = transaction;

            sqlCommand4.CommandText =
                "INSERT INTO dbo.EVENTS (AGGREGATEID, DATA, VERSION, TYPE, creationdate) VALUES(@id, @data, @version, @typeHint, @creationdate)";

            // Attention here: You must add the parameters in the same sequence as in the query statement, otherwise it won't work.
            sqlCommand4.Parameters.Clear();
            sqlCommand4.Parameters.Add(physicalId.ToSqlParameter("@id"));
            sqlCommand4.Parameters.Add(eventData.eventData.ToSqlParameter("@data"));
            sqlCommand4.Parameters.Add((++eventCount).ToSqlParameter("@version"));
            sqlCommand4.Parameters.Add(@event.GetType()
                .Name.ToSqlParameter("@typeHint"));
            sqlCommand4.Parameters.Add(ApplicationTime.Current.ToSqlParameter("@creationdate"));

            var rows = await sqlCommand4.ExecuteNonQueryAsync();

            if (rows <= 0)
            {
                throw new ProviderException("Failing inserting subsequent event record.");
            }
        }

        return eventCount;
    }

    private static async Task<Guid> CreateNewAggregateStreamAsync(IEventSourcingBasedAggregate aggregate, SqlConnection connection, SqlTransaction transaction)
    {
        var physicalId = Guid.NewGuid();

        await using var sqlCommand2 = connection.CreateCommand();
        sqlCommand2.Transaction = transaction;
        sqlCommand2.CommandText = "INSERT INTO dbo.AGGREGATES (AGGREGATEID, TYPE, VERSION) VALUES(@id, @type, @version)";

        sqlCommand2.Parameters.Clear();
        sqlCommand2.Parameters.Add(physicalId.ToSqlParameter("@id"));
        sqlCommand2.Parameters.Add(aggregate.GetType()
            .AssemblyQualifiedName!.ToSqlParameter("@type"));
        sqlCommand2.Parameters.Add(0.ToSqlParameter("@version"));

        var rows = await sqlCommand2.ExecuteNonQueryAsync();

        if (rows <= 0)
        {
            throw new ProviderException("Failing inserting first event record.");
        }

        await using var sqlCommand3 = connection.CreateCommand();
        sqlCommand3.Transaction = transaction;
        sqlCommand3.CommandText = "INSERT INTO dbo.MAPPINGS (AGGREGATEID, TYPE, [KEY]) VALUES(@id, @type, @key)";

        sqlCommand3.Parameters.Clear();
        sqlCommand3.Parameters.Add(physicalId.ToSqlParameter("@id"));
        sqlCommand3.Parameters.Add(aggregate.GetType()
            .AssemblyQualifiedName!.ToSqlParameter("@type"));
        sqlCommand3.Parameters.Add(aggregate.SerializedId.ToSqlParameter("@key"));

        rows = await sqlCommand3.ExecuteNonQueryAsync();

        if (rows <= 0)
        {
            throw new ProviderException("Failing inserting new mapping record.");
        }

        return physicalId;
    }

    private static async Task EnsureUniqueConstraintsAsync(Guid physicalId, IEventSourcingBasedAggregate aggregate, SqlConnection connection, SqlTransaction transaction)
    {
        var uniqueProperties = aggregate.GetType().GetProperties().Where(
            prop => Attribute.IsDefined(prop, typeof(UniqueDomainPropertyAttribute)));

        await using var command1 = connection.CreateCommand();
        command1.Transaction = transaction;
        await using var command2 = connection.CreateCommand();
        command2.Transaction = transaction;
        await using var command3 = connection.CreateCommand();
        command3.Transaction = transaction;

        if (aggregate.GetStreamState() == StreamState.StreamClosed)
        {
            await using var command4 = connection.CreateCommand();
            command4.Transaction = transaction;

            command4.CommandText =
                "DELETE FROM dbo.UNIQUE_CONSTRAINTS WHERE AGGREGATEID = @id";

            command4.Parameters.Clear();
            command4.Parameters.Add(physicalId.ToSqlParameter("@id"));

            await command4.ExecuteNonQueryAsync();
        }
        else
        {
            foreach (var uniqueProperty in uniqueProperties)
            {
                if (uniqueProperty.GetValue(aggregate) == null)
                {
                    continue; // We allow unique properties to be null
                }

                // 1. does the aggregate have a value in the constraints table?
                command1.CommandText =
                    "SELECT COUNT(*) FROM dbo.UNIQUE_CONSTRAINTS WHERE AGGREGATEID = @id AND AGGREGATEPROPERTY = @property";

                command1.Parameters.Clear();
                command1.Parameters.Add(physicalId.ToSqlParameter("@id"));
                command1.Parameters.Add(uniqueProperty.Name.ToSqlParameter("@property"));

                var countValues = await command1.ExecuteScalarAsync();
                var hasValue = 0;

                if (countValues != null)
                {
                    hasValue = Convert.ToInt32(countValues, CultureInfo.InvariantCulture);
                }

                switch (hasValue)
                {
                    // 1.a
                    case > 0:
                    {
                        command2.CommandText =
                            "UPDATE dbo.UNIQUE_CONSTRAINTS SET PROPERTYVALUE = @value WHERE AGGREGATEID = @id AND AGGREGATEPROPERTY = @property";

                        command2.Parameters.Clear();

                        command2.Parameters.Add(uniqueProperty.GetValue(aggregate)!.ToString()!.ToSqlParameter("@value"));
                        command2.Parameters.Add(physicalId.ToSqlParameter("@id"));
                        command2.Parameters.Add(uniqueProperty.Name.ToSqlParameter("@property"));

                        var updated = await command2.ExecuteNonQueryAsync();

                        if (updated <= 0)
                        {
                            throw new ProviderException("Failing inserting unique constraint value.");
                        }

                        break;
                    }
                    // 1.b if it does not insert the value
                    case 0:
                    {
                        command3.CommandText =
                            @"INSERT INTO dbo.UNIQUE_CONSTRAINTS (AGGREGATEID, AGGREGATETYPE, AGGREGATEPROPERTY, PROPERTYVALUE)
                                values(@id, @type, @property, @value)";

                        command3.Parameters.Clear();
                        command3.Parameters.Add(physicalId.ToSqlParameter("@id"));
                        command3.Parameters.Add(aggregate.GetType().AssemblyQualifiedName!.ToSqlParameter("@type"));
                        command3.Parameters.Add(uniqueProperty.Name.ToSqlParameter("@property"));
                        command3.Parameters.Add(uniqueProperty.GetValue(aggregate)!.ToString()!.ToSqlParameter("@value"));

                        var inserted = await command3.ExecuteNonQueryAsync();

                        if (inserted <= 0)
                        {
                            throw new ProviderException("Failing inserting unique constraint value.");
                        }

                        break;
                    }
                }
            }
        }
    }
}
