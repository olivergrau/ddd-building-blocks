using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using DDD.BuildingBlocks.Core.Exception;

namespace DDD.BuildingBlocks.Core.Event.Serialization;

public sealed class SystemTextJsonEventCodec : IEventCodec
{
    private static readonly string[] LegacyMetadataProperties =
    [
        nameof(IDomainEvent.TargetVersion),
        nameof(IDomainEvent.SerializedAggregateId),
        nameof(IDomainEvent.EventCommittedTimestamp),
        nameof(IDomainEvent.ClassVersion),
        nameof(DomainEvent.CorrelationId),
        nameof(DomainEvent.FullType)
    ];

    private readonly IEventTypeRegistry _eventTypes;
    private readonly IReadOnlyDictionary<(string EventType, int SourceVersion), IEventUpcaster> _upcasters;
    private readonly JsonSerializerOptions _serializerOptions;

    public SystemTextJsonEventCodec(
        IEventTypeRegistry eventTypes,
        IEnumerable<IEventUpcaster>? upcasters = null,
        JsonSerializerOptions? serializerOptions = null)
    {
        _eventTypes = eventTypes ?? throw new ArgumentNullException(nameof(eventTypes));
        _serializerOptions = serializerOptions is null
            ? new JsonSerializerOptions(JsonSerializerDefaults.Web)
            : new JsonSerializerOptions(serializerOptions);
        _upcasters = BuildUpcasterMap(upcasters ?? []);
    }

    public EventEnvelope Encode(IDomainEvent domainEvent, EventEnvelopeMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(metadata);

        var registration = _eventTypes.GetByClrType(domainEvent.GetType());

        if (domainEvent.ClassVersion != registration.CurrentSchemaVersion)
        {
            throw new EventSerializationException(
                $"Event class version {domainEvent.ClassVersion} does not match registered schema version {registration.CurrentSchemaVersion} for '{registration.EventType}'.");
        }

        if (domainEvent.TargetVersion + 1 != metadata.StreamVersion)
        {
            throw new EventSerializationException(
                $"Event target version {domainEvent.TargetVersion} does not match envelope stream version {metadata.StreamVersion}.");
        }

        if (domainEvent.SerializedAggregateId is not null &&
            !string.Equals(domainEvent.SerializedAggregateId, metadata.StreamId, StringComparison.Ordinal))
        {
            throw new EventSerializationException(
                $"Event stream ID '{domainEvent.SerializedAggregateId}' does not match envelope stream ID '{metadata.StreamId}'.");
        }

        try
        {
            var payload = JsonSerializer.SerializeToNode(domainEvent, domainEvent.GetType(), _serializerOptions) as JsonObject
                ?? throw new EventSerializationException("A domain event must serialize to a JSON object.");

            foreach (var property in LegacyMetadataProperties)
            {
                payload.Remove(GetSerializedPropertyName(property));
            }

            using var document = JsonDocument.Parse(payload.ToJsonString(_serializerOptions));
            return new EventEnvelope(
                metadata.EventId,
                metadata.StreamId,
                metadata.AggregateType,
                metadata.StreamVersion,
                metadata.GlobalPosition,
                registration.EventType,
                registration.CurrentSchemaVersion,
                metadata.OccurredAt,
                metadata.CommittedAt,
                metadata.CorrelationId ?? domainEvent.CorrelationId,
                metadata.CausationId,
                metadata.CommandId,
                metadata.Actor,
                metadata.TurnId,
                document.RootElement);
        }
        catch (EventSerializationException)
        {
            throw;
        }
        catch (System.Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new EventSerializationException(
                $"Could not serialize event type '{registration.EventType}'.", exception);
        }
    }

    public IDomainEvent Decode(EventEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var registration = _eventTypes.GetByEventType(envelope.EventType);
        var payload = JsonNode.Parse(envelope.Payload.GetRawText())
            ?? throw new EventSerializationException($"Event payload for '{envelope.EventType}' is empty.");
        var schemaVersion = envelope.SchemaVersion;

        if (schemaVersion > registration.CurrentSchemaVersion)
        {
            throw new UnsupportedEventSchemaVersionException(envelope.EventType, schemaVersion);
        }

        while (schemaVersion < registration.CurrentSchemaVersion)
        {
            if (!_upcasters.TryGetValue((envelope.EventType, schemaVersion), out var upcaster))
            {
                throw new UnsupportedEventSchemaVersionException(
                    envelope.EventType,
                    schemaVersion,
                    $"No upcaster from schema version {schemaVersion} is registered for event type '{envelope.EventType}'.");
            }

            payload = upcaster.Upcast(payload)
                ?? throw new EventSerializationException(
                    $"Upcaster for '{envelope.EventType}' schema version {schemaVersion} returned no payload.");
            schemaVersion = upcaster.TargetSchemaVersion;
        }

        try
        {
            var payloadObject = payload as JsonObject
                ?? throw new EventSerializationException("A domain event payload must be a JSON object.");

            payloadObject[GetSerializedPropertyName(nameof(IDomainEvent.TargetVersion))] = envelope.StreamVersion - 1;
            payloadObject[GetSerializedPropertyName(nameof(IDomainEvent.SerializedAggregateId))] = envelope.StreamId;
            payloadObject[GetSerializedPropertyName(nameof(IDomainEvent.EventCommittedTimestamp))] =
                (envelope.CommittedAt ?? envelope.OccurredAt).UtcDateTime;
            payloadObject[GetSerializedPropertyName(nameof(IDomainEvent.ClassVersion))] = registration.CurrentSchemaVersion;
            payloadObject[GetSerializedPropertyName(nameof(DomainEvent.CorrelationId))] = envelope.CorrelationId;

            return JsonSerializer.Deserialize(payloadObject.ToJsonString(_serializerOptions), registration.ClrType, _serializerOptions)
                       as IDomainEvent
                   ?? throw new EventSerializationException(
                       $"Deserializer returned no event for type '{envelope.EventType}'.");
        }
        catch (EventSerializationException)
        {
            throw;
        }
        catch (System.Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException)
        {
            throw new EventSerializationException(
                $"Could not deserialize event type '{envelope.EventType}' at schema version {schemaVersion}.", exception);
        }
    }

    private string GetSerializedPropertyName(string propertyName)
    {
        return _serializerOptions.PropertyNamingPolicy?.ConvertName(propertyName) ?? propertyName;
    }

    private static IReadOnlyDictionary<(string EventType, int SourceVersion), IEventUpcaster> BuildUpcasterMap(
        IEnumerable<IEventUpcaster> upcasters)
    {
        var result = new Dictionary<(string EventType, int SourceVersion), IEventUpcaster>();

        foreach (var upcaster in upcasters)
        {
            ArgumentNullException.ThrowIfNull(upcaster);
            ArgumentException.ThrowIfNullOrWhiteSpace(upcaster.EventType);

            if (upcaster.SourceSchemaVersion < 1 || upcaster.TargetSchemaVersion != upcaster.SourceSchemaVersion + 1)
            {
                throw new EventTypeRegistrationException(
                    $"Upcaster for '{upcaster.EventType}' must advance exactly one positive schema version.");
            }

            if (!result.TryAdd((upcaster.EventType, upcaster.SourceSchemaVersion), upcaster))
            {
                throw new EventTypeRegistrationException(
                    $"Multiple upcasters from schema version {upcaster.SourceSchemaVersion} are registered for '{upcaster.EventType}'.");
            }
        }

        return result;
    }
}
