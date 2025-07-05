using DDD.BuildingBlocks.Core.ErrorHandling;

namespace DDD.BuildingBlocks.Core.Exception
{
    public class ReadFromReadModelException(string message, System.Exception? inner = null)
        : ClassifiedErrorException(new ClassificationInfo(message, ErrorOrigin.ReadModel, ErrorClassification.Infrastructure), inner);
}