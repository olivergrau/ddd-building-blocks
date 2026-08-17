using DDD.BuildingBlocks.Core.ErrorHandling;

namespace DDD.BuildingBlocks.Core.Exception;

public sealed class EventStoreProviderException(
    string message,
    bool isTransient,
    System.Exception? innerException = null)
    : ClassifiedErrorException(new ClassificationInfo(
        message,
        ErrorOrigin.Infrastructure,
        isTransient ? ErrorClassification.TransientProviderFailure : ErrorClassification.PermanentProviderFailure),
        innerException)
{
    public bool IsTransient { get; } = isTransient;
}
