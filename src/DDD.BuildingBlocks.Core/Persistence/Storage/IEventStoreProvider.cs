using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Core.Persistence.Storage;

public interface IEventStoreProvider
{
    Task<AppendEventsResult> AppendAsync(
        string streamId,
        string aggregateType,
        long expectedVersion,
        IReadOnlyCollection<EventEnvelope> events,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EventEnvelope>> ReadStreamAsync(
        string streamId,
        string aggregateType,
        long fromStreamVersion,
        int maxCount,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EventEnvelope>> ReadCommittedFeedAsync(
        long afterGlobalPosition,
        int maxCount,
        CancellationToken cancellationToken);
}
