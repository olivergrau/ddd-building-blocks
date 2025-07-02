// ReSharper disable LogMessageIsSentenceProblem

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DDD.BuildingBlocks.MSSQLPackage.Service;

using System.Data;
using System.Globalization;
using Core.Attribute;
using Core.Event;
using Hosting.Background.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Provider;

/// <summary>
///     Gets unpublished events from the storage table and publishes them.
/// </summary>
/// <remarks>
///     Attention!
///     Uses an offset for tracking of published events.
///     The initial offset value must exist in database otherwise the update will fail.
/// </remarks>
public class EventProcessingServiceBackgroundWorker : IBackgroundServiceWorker
{
    private const int EventsProcessedAtOnce = 500;

    private readonly string _workerId;
    private readonly IDomainEventHandler _eventHandler;
    private readonly EventProcessingServiceBackgroundWorkerSettings _settings;

    private readonly ILogger<EventProcessingServiceBackgroundWorker> _log;

    public EventProcessingServiceBackgroundWorker(
        string workerId, OptionsSetNames optionsSet, IOptionsMonitor<EventProcessingServiceBackgroundWorkerSettings> settingsMonitor,
        IDomainEventHandler eventHandler, ILogger<EventProcessingServiceBackgroundWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(settingsMonitor, nameof(settingsMonitor));
        if (string.IsNullOrWhiteSpace(workerId))
        {
            throw new ArgumentException("workerId is null or empty", nameof(workerId));
        }

        _log = logger;
        _workerId = workerId;
        _settings = settingsMonitor.Get(optionsSet.ToString());
        _eventHandler = eventHandler;

        _log.LogDebug("[{WorkerId}]: OptionSet: {OptionSet} will be used", workerId, optionsSet.ToString());
    }

    public async Task ProcessAsync(CancellationToken? cancellationToken = null)
    {
        if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
        {
            _log.LogDebug($"Connection string is empty. Probably the configuration system has not been initialized yet");
            return;
        }

        await using var connection = new SqlConnection(_settings.ConnectionString);
        await using var command = connection.CreateCommand();
        await connection.OpenAsync();

        _log.LogDebug("[{WorkerId}]: Sql connection opened", _workerId);

        await using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        command.Transaction = transaction;

        try
        {
            if (_settings.OnlyAllowSendingForCategories != null && _settings.OnlyAllowSendingForCategories.Any())
            {
                _log.LogDebug("[{WorkerId}]: Restricting categories detected: {Categories}", _workerId, string.Join(", ", _settings.OnlyAllowSendingForCategories));
            }

            // https://learn.microsoft.com/en-us/answers/questions/99361/what-is-rowlock-and-what-it-does
            command.CommandText = "SELECT offset FROM dbo.OFFSET WITH(ROWLOCK) WHERE WORKERID = '" + _workerId + "'";
            var offsetRaw = await command.ExecuteScalarAsync();

            var offset = 0;

            if (offsetRaw != null)
            {
                offset = Convert.ToInt32(offsetRaw, CultureInfo.InvariantCulture);
            }
            else
            {
                command.CommandText = "INSERT INTO dbo.OFFSET (OFFSET,WORKERID) VALUES('0','" + _workerId + "')";
                await command.ExecuteNonQueryAsync();
                _log.Log(LogLevel.Debug, "[{WorkerId}]: First time initialization of worker id based offset value: 0", _workerId);
            }

            _log.Log(LogLevel.Debug, "[{WorkerId}]: Got offset = {Offset} from event store", _workerId, offset);

            command.CommandText = @"SELECT E.AGGREGATEID, E.DATA, E.VERSION, E.TYPE, CREATIONDATE, [KEY]
                                FROM dbo.EVENTS E INNER JOIN dbo.MAPPINGS M ON E.AGGREGATEID = M.AGGREGATEID ORDER BY CREATIONDATE, VERSION
                                OFFSET " + offset + " ROWS FETCH NEXT " + EventsProcessedAtOnce + " ROWS ONLY";

            var reader = await command.ExecuteReaderAsync();

            var inspectedRows = 0;
            var domainEvents = new List<IDomainEvent>();
            while (await reader.ReadAsync())
            {
                EventStorageProviderHelper.ReconstituteEvent(reader, domainEvents);
                inspectedRows++;
            }

            await reader.CloseAsync();
            _log.Log(LogLevel.Debug, "[{WorkerId}]: Got {Number} events to process", _workerId, domainEvents.Count);

            foreach (var @event in domainEvents)
            {
                // get attribute for event
                var describingAttribute =
                    (DomainEventTypeAttribute) Attribute.GetCustomAttribute(@event.GetType(), typeof (DomainEventTypeAttribute))!;

                _log.LogDebug("[{WorkerId}]: DomainEvent Category: {Category}", _workerId, describingAttribute.Category);

                // now either there are no types in allowedTypes or we have to check the type of the event if it is in the allowedTypes collection
                if(_settings.OnlyAllowSendingForCategories != null && _settings.OnlyAllowSendingForCategories.Length != 0)
                {
                    _log.LogDebug("[{WorkerId}]: Domain Events must have at least one of the following categories: {Categories}",
                        _workerId, string.Join(",", _settings.OnlyAllowSendingForCategories));
                }

                if(_settings.OnlyAllowSendingForCategories == null || _settings.OnlyAllowSendingForCategories.Length == 0
                   || describingAttribute.Category != null && _settings.OnlyAllowSendingForCategories.Contains(describingAttribute.Category))
                {
                    await _eventHandler.HandleAsync(@event);
                }
                else
                {
                    _log.LogDebug("[{WorkerId}]: Event with Id: {EventId} is not allowed to be handled and was skipped", _workerId, @event.FullType);
                }
            }

            if (inspectedRows > 0)
            {
                command.CommandText = "UPDATE dbo.OFFSET SET OFFSET = @offset WHERE WORKERID = '" + _workerId + "'";
                command.Parameters.Clear();

                command.Parameters.Add((offset + inspectedRows).ToSqlParameter("@offset"));

                await command.ExecuteNonQueryAsync();

                _log.Log(LogLevel.Debug, "[{WorkerId}]: Updated OFFSET with {Offset}", _workerId, offset + inspectedRows);
            }

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction?.Rollback();
            throw;
        }
    }
}
