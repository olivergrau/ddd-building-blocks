namespace DDD.BuildingBlocks.Core.Exception;

public class UnsupportedEventSchemaVersionException(string eventType, int schemaVersion, string? message = null)
    : System.Exception(message ?? $"Schema version {schemaVersion} is not supported for event type '{eventType}'.")
{
    public string EventType { get; } = eventType;
    public int SchemaVersion { get; } = schemaVersion;
}
