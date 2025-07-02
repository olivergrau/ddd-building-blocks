namespace DDD.BuildingBlocks.Core.ErrorHandling
{
    public class ClassifiedErrorException(ClassificationInfo errorInfo, System.Exception? originException = null)
        : System.Exception(errorInfo.Message, originException)
    {
        public ClassificationInfo ErrorInfo { get; } = errorInfo;
    }
}