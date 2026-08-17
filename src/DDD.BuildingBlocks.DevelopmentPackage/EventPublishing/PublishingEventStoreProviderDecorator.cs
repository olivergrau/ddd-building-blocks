using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Event.Serialization;
using DDD.BuildingBlocks.Core.Persistence.Storage;

namespace DDD.BuildingBlocks.DevelopmentPackage.EventPublishing;

public sealed class PublishingEventStoreProviderDecorator(
    IEventStoreProvider inner,
    IEventCodec eventCodec,
    EventPublishingTable publishingTable) : IEventStoreProvider
{
    public async Task<AppendEventsResult> AppendAsync(
        string streamId,
        string aggregateType,
        long expectedVersion,
        IReadOnlyCollection<EventEnvelope> events,
        CancellationToken cancellationToken)
    {
        var result = await inner.AppendAsync(
            streamId, aggregateType, expectedVersion, events, cancellationToken);

        foreach (var envelope in events)
        {
            publishingTable.Enqueue(eventCodec.Decode(envelope));
        }

        return result;
    }

    public Task<IReadOnlyList<EventEnvelope>> ReadStreamAsync(
        string streamId,
        string aggregateType,
        long fromStreamVersion,
        int maxCount,
        CancellationToken cancellationToken) =>
        inner.ReadStreamAsync(streamId, aggregateType, fromStreamVersion, maxCount, cancellationToken);

    public Task<IReadOnlyList<EventEnvelope>> ReadCommittedFeedAsync(
        long afterGlobalPosition,
        int maxCount,
        CancellationToken cancellationToken) =>
        inner.ReadCommittedFeedAsync(afterGlobalPosition, maxCount, cancellationToken);
}
