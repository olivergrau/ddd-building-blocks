using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.AzurePackage.DomainEventHandler;

using System.Diagnostics;
using System.Globalization;
using Core.Persistence.SnapshotSupport;
using Core.Persistence.Storage;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Options;

public class ServiceBusIntegrationEventSender(
    IOptionsMonitor<ServiceBusIntegrationEventSenderOptions> configuration,
    ServiceBusClient client,
    ISnapshotCreationService snapshotCreationService,
    IStringStorageService stringStorageService,
    ILoggerFactory loggerFactory,
    TelemetryClient telemetryClient
) : IDomainEventHandler
{
    private readonly ServiceBusSender _sender = client.CreateSender(configuration.CurrentValue.QueueName ?? throw new Exception("QueueName is empty"));
    private readonly ILogger _log = loggerFactory.CreateLogger<ServiceBusIntegrationEventSender>();
    private readonly string _queueName = configuration.CurrentValue.QueueName;

    public async Task HandleAsync(IDomainEvent @event)
    {
        var snapshot = await snapshotCreationService.CreateSnapshotFrom(@event.SerializedAggregateId!, @event.TargetVersion + 1);

        if (snapshot == null)
        {
            _log.LogDebug("No snapshot could be created for aggregate with id: {AggregateId}", @event.SerializedAggregateId);
            return;
        }

        var messageClaimId = Guid.NewGuid();
        var messageContent = JsonSerializer.Serialize(snapshot,
            snapshot.GetType(),
            new JsonSerializerOptions
            {
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            });

        await stringStorageService.SaveAsync(messageContent, messageClaimId.ToString());

        var data = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new MessageClaim(messageClaimId, messageContent)));

        var activity = new Activity($"{nameof(ServiceBusIntegrationEventSender)}.SendMessage");
        activity.SetParentId(@event.CorrelationId!);

        using var operation = telemetryClient.StartOperation<RequestTelemetry>("Process", activity.RootId, activity.ParentId);

        var message = new ServiceBusMessage(data)
        {
            Subject = $"Snapshot for Aggregate {@event.SerializedAggregateId}",
            ContentType = "application/json",
            ApplicationProperties =
            {
                { "CorrelationId", @event.CorrelationId },
                { "MessageClaimId", messageClaimId.ToString() },
                { "AggregateType", snapshot.AggregateTypeIdentifier },
                { "AggregateId", snapshot.SerializedAggregateId },
                { "AggregateVersion", snapshot.Version.ToString(CultureInfo.InvariantCulture) },
            }
        };
        telemetryClient.TrackTrace(
            $"Sending message: CorrelationId: {@event.CorrelationId}, AggregateType: {snapshot.AggregateTypeIdentifier}, AggregateId: {snapshot.SerializedAggregateId}, AggregateVersion: {snapshot.Version.ToString(CultureInfo.InvariantCulture)}");

        await _sender.SendMessageAsync(message);

        _log.LogDebug("Message {Subject} was sent to queue: {QueueName}. Correlation: {CorrelationId}, Diagnostics: {Diagnostic-Id}",
            message.Subject,
            _queueName,
            @event.CorrelationId,
            activity.RootId);

        telemetryClient.TrackTrace($"Message was sent: {message.MessageId}, CorrelationId: {message.CorrelationId}");
    }
}
