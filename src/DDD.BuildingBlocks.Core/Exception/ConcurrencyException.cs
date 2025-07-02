using System;

namespace DDD.BuildingBlocks.Core.Exception
{
    [Serializable]
    public class ConcurrencyException(string message) : System.Exception(message);
}
