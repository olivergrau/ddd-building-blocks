namespace DDD.BuildingBlocks.Core.Exception
{
    public class DomainServiceException(string message, System.Exception? inner = null) : System.Exception(message, inner);
}