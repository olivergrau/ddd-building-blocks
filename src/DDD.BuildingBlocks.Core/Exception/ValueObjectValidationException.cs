namespace DDD.BuildingBlocks.Core.Exception;

public class ValueObjectValidationException(object valueObjectName, string propertyName, object value, string optionalHint = "")
    : System.Exception($"ValueObject [{valueObjectName}]: property [{propertyName} = {value}] validation error: {optionalHint}")
{
    public object ValueObjectName { get; } = valueObjectName;
}