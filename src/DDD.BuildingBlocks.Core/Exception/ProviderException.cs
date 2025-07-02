namespace DDD.BuildingBlocks.Core.Exception
{
    public class ProviderException(string message, System.Exception? inner = null) : System.Exception(message, inner);
}