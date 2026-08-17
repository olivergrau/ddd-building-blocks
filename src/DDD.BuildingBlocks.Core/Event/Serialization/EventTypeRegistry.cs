using System;
using System.Collections.Generic;
using DDD.BuildingBlocks.Core.Exception;

namespace DDD.BuildingBlocks.Core.Event.Serialization;

public sealed class EventTypeRegistry : IEventTypeRegistry
{
    private readonly Dictionary<string, EventTypeRegistration> _byEventType = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, EventTypeRegistration> _byClrType = [];
    private readonly object _sync = new();

    public EventTypeRegistry Register<TEvent>(string eventType, int currentSchemaVersion = 1)
        where TEvent : IDomainEvent
    {
        return Register(new EventTypeRegistration(eventType, typeof(TEvent), currentSchemaVersion));
    }

    public EventTypeRegistry Register(EventTypeRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        lock (_sync)
        {
            if (_byEventType.TryGetValue(registration.EventType, out var eventTypeRegistration))
            {
                throw new EventTypeRegistrationException(
                    $"Event type key '{registration.EventType}' is already registered for {eventTypeRegistration.ClrType.FullName}.");
            }

            if (_byClrType.TryGetValue(registration.ClrType, out var clrTypeRegistration))
            {
                throw new EventTypeRegistrationException(
                    $"CLR type {registration.ClrType.FullName} is already registered as '{clrTypeRegistration.EventType}'.");
            }

            _byEventType.Add(registration.EventType, registration);
            _byClrType.Add(registration.ClrType, registration);
        }

        return this;
    }

    public EventTypeRegistration GetByEventType(string eventType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        lock (_sync)
        {
            return _byEventType.TryGetValue(eventType, out var registration)
                ? registration
                : throw new UnknownEventTypeException(eventType);
        }
    }

    public EventTypeRegistration GetByClrType(Type clrType)
    {
        ArgumentNullException.ThrowIfNull(clrType);

        lock (_sync)
        {
            return _byClrType.TryGetValue(clrType, out var registration)
                ? registration
                : throw new UnknownEventTypeException(clrType.FullName ?? clrType.Name);
        }
    }
}
