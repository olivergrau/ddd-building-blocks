namespace DDD.BuildingBlocks.Core.ErrorHandling
{
    public static class ExceptionExtensions
    {
        public static ClassifiedErrorException ToStructurizedException(this System.Exception exception, ErrorOrigin origin,
            ErrorClassification classification)
        {
            return new ClassifiedErrorException(new ClassificationInfo(exception.Message, origin, classification),
                exception);
        }

        public static ClassifiedErrorException ToRepositoryOriginatedException(this System.Exception exception)
        {
            return new ClassifiedErrorException(new ClassificationInfo(exception.Message, ErrorOrigin.Repository,
                ErrorClassification.Infrastructure));
        }
    }
}