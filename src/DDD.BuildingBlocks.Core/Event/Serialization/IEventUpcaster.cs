using System.Text.Json.Nodes;

namespace DDD.BuildingBlocks.Core.Event.Serialization;

public interface IEventUpcaster
{
    string EventType { get; }
    int SourceSchemaVersion { get; }
    int TargetSchemaVersion { get; }
    JsonNode Upcast(JsonNode payload);
}
