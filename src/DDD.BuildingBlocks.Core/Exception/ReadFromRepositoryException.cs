using DDD.BuildingBlocks.Core.ErrorHandling;
using DDD.BuildingBlocks.Core.Exception.Constants;

namespace DDD.BuildingBlocks.Core.Exception
{
    public class ReadFromRepositoryException(System.Exception inner) : ClassifiedErrorException(
        new ClassificationInfo(HandlerErrors.ErrorReadingFromRepository,
            ErrorOrigin.Repository,
            ErrorClassification.Infrastructure),
        inner);
}