namespace DDD.BuildingBlocks.Core.Exception;

public class EventSerializationException(string message, System.Exception? innerException = null)
    : System.Exception(message, innerException);
