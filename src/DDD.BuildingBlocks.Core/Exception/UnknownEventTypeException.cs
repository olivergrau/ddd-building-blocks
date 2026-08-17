namespace DDD.BuildingBlocks.Core.Exception;

public class UnknownEventTypeException(string eventType)
    : System.Exception($"Event type '{eventType}' is not registered.")
{
    public string EventType { get; } = eventType;
}
