using DDD.BuildingBlocks.Core.ErrorHandling;

namespace DDD.BuildingBlocks.Core.Exception;

public sealed class EventStoreConcurrencyException(
    string streamId,
    long expectedVersion,
    long actualVersion)
    : ClassifiedErrorException(new ClassificationInfo(
        $"Stream '{streamId}' has version {actualVersion}, but version {expectedVersion} was expected.",
        ErrorOrigin.Infrastructure,
        ErrorClassification.ConcurrencyConflict))
{
    public string StreamId { get; } = streamId;
    public long ExpectedVersion { get; } = expectedVersion;
    public long ActualVersion { get; } = actualVersion;
}
