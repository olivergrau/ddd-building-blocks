using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.AzurePackage.DomainEventHandler;

using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Options;

public class ServiceBusDomainEventSender(
    IOptionsMonitor<ServiceBusDomainEventSenderOptions> configuration,
    ServiceBusClient client,
    ILoggerFactory loggerFactory,
    TelemetryClient telemetryClient
) : IDomainEventHandler
{
    private readonly ServiceBusSender _sender = client.CreateSender(configuration.CurrentValue.QueueName);
    private readonly ILogger _log = loggerFactory.CreateLogger<ServiceBusDomainEventSender>();

    public async Task HandleAsync(IDomainEvent @event)
    {
        var data = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(@event, @event.GetType(), new JsonSerializerOptions
            {
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            }));

         var activity = new Activity($"{nameof(ServiceBusDomainEventSender)}.SendMessage");
         activity.SetParentId(@event.CorrelationId!);

         using var operation = telemetryClient.StartOperation<RequestTelemetry>("Process", activity.RootId, activity.ParentId);

        var message = new ServiceBusMessage(data)
        {
            Subject = $"Domain Event for Aggregate {@event.SerializedAggregateId}",
            ContentType = "application/json",
            ApplicationProperties =
            {
                { "CorrelationId", @event.CorrelationId },
                { "SourceAggregateId", @event.SerializedAggregateId },
                { "EventName", @event.GetType().Name },
                { "EventFullType", @event.GetType().FullName },
            },
        };

        telemetryClient.TrackTrace(
            $"Sending message: CorrelationId: {@event.CorrelationId}, AggregateId: {@event.SerializedAggregateId}");

        await _sender.SendMessageAsync(message);

        _log.LogDebug(
                "Message {Subject} was sent. Correlation: {CorrelationId}, Diagnostics: {Diagnostic-Id}",
                message.Subject, @event.CorrelationId, activity.RootId);

        telemetryClient.TrackTrace($"Message was sent: {message.MessageId}, CorrelationId: {message.CorrelationId}");
    }
}
