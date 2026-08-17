using System;

namespace DDD.BuildingBlocks.Core.Event.Serialization;

public sealed record EventTypeRegistration
{
    public EventTypeRegistration(string eventType, Type clrType, int currentSchemaVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(clrType);
        ArgumentOutOfRangeException.ThrowIfLessThan(currentSchemaVersion, 1);

        if (!typeof(IDomainEvent).IsAssignableFrom(clrType))
        {
            throw new ArgumentException($"Type {clrType.FullName} does not implement {nameof(IDomainEvent)}.", nameof(clrType));
        }

        EventType = eventType;
        ClrType = clrType;
        CurrentSchemaVersion = currentSchemaVersion;
    }

    public string EventType { get; }
    public Type ClrType { get; }
    public int CurrentSchemaVersion { get; }
}
