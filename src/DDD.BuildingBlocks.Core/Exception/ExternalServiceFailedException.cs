using System;

namespace DDD.BuildingBlocks.Core.Exception
{
    [Serializable]
    public class ExternalServiceFailedException(string message, System.Exception? inner = null) : System.Exception(message, inner);
}