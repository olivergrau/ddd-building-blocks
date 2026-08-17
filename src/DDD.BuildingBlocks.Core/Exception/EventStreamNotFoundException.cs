using DDD.BuildingBlocks.Core.ErrorHandling;

namespace DDD.BuildingBlocks.Core.Exception;

public sealed class EventStreamNotFoundException(string streamId)
    : ClassifiedErrorException(new ClassificationInfo(
        $"Stream '{streamId}' does not exist.",
        ErrorOrigin.Infrastructure,
        ErrorClassification.StreamNotFound))
{
    public string StreamId { get; } = streamId;
}
