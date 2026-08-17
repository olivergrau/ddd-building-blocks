using System;

namespace DDD.BuildingBlocks.Core.Event.Serialization;

public interface IEventTypeRegistry
{
    EventTypeRegistration GetByEventType(string eventType);
    EventTypeRegistration GetByClrType(Type clrType);
}
