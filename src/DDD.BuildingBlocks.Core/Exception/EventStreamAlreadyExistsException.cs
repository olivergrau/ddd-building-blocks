using DDD.BuildingBlocks.Core.ErrorHandling;

namespace DDD.BuildingBlocks.Core.Exception;

public sealed class EventStreamAlreadyExistsException(string streamId, long actualVersion)
    : ClassifiedErrorException(new ClassificationInfo(
        $"Stream '{streamId}' already exists at version {actualVersion}.",
        ErrorOrigin.Infrastructure,
        ErrorClassification.StreamAlreadyExists))
{
    public string StreamId { get; } = streamId;
    public long ActualVersion { get; } = actualVersion;
}
