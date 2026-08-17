using System;
using DDD.BuildingBlocks.Core.ErrorHandling;

namespace DDD.BuildingBlocks.Core.Exception;

public sealed class DuplicateEventIdException(Guid eventId)
    : ClassifiedErrorException(new ClassificationInfo(
        $"Event ID '{eventId}' is already committed.",
        ErrorOrigin.Infrastructure,
        ErrorClassification.PermanentProviderFailure))
{
    public Guid EventId { get; } = eventId;
}
