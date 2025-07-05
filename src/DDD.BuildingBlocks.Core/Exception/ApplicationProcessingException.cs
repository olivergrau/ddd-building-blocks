using DDD.BuildingBlocks.Core.ErrorHandling;

namespace DDD.BuildingBlocks.Core.Exception
{
    public class ApplicationProcessingException(string message, System.Exception? inner = null)
        : ClassifiedErrorException(new ClassificationInfo(message, ErrorOrigin.ApplicationLevel, ErrorClassification.ProcessingError), inner);
}