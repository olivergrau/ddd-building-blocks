namespace DDD.BuildingBlocks.Core.Exception;

public class EntityValidationException(object entityId, string propertyName, object? value, string optionalHint = "")
    : System.Exception($"Entity [{entityId}]: property [{propertyName} = {value}] validation error: {optionalHint}")
{
    public object EntityId { get; } = entityId;
}