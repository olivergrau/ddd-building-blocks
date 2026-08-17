using System;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Persistence.Storage;

namespace DDD.BuildingBlocks.Core.Projection;

public sealed class ProjectionRunner(
    IEventStoreProvider eventStoreProvider,
    IProjectionCheckpointStore checkpointStore)
{
    public async Task<ProjectionRunResult> RunBatchAsync(
        IEventProjection projection,
        int batchSize,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        var checkpoint = await checkpointStore.GetAsync(projection.Key, cancellationToken).ConfigureAwait(false);
        var feed = await eventStoreProvider
            .ReadCommittedFeedAsync(checkpoint.LastProcessedPosition, batchSize, cancellationToken)
            .ConfigureAwait(false);
        var applied = 0;
        var lastPosition = checkpoint.LastProcessedPosition;

        foreach (var envelope in feed)
        {
            var position = envelope.GlobalPosition ?? throw new InvalidOperationException(
                "Committed feed envelopes must have a global position.");
            try
            {
                if (await checkpointStore.ProcessAsync(
                        projection.Key,
                        envelope,
                        (context, token) => projection.ApplyAsync(envelope, context, token),
                        cancellationToken)
                    .ConfigureAwait(false))
                {
                    applied++;
                }

                lastPosition = position;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception exception)
            {
                await checkpointStore
                    .MarkFailedAsync(projection.Key, position, exception.ToString(), CancellationToken.None)
                    .ConfigureAwait(false);
                throw;
            }
        }

        return new ProjectionRunResult(feed.Count, applied, lastPosition, feed.Count == batchSize);
    }

    public Task RebuildAsync(
        IEventProjection projection,
        Func<ProjectionTransactionContext, CancellationToken, Task> resetReadModel,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(resetReadModel);
        return checkpointStore.ResetAsync(projection.Key, resetReadModel, cancellationToken);
    }

    public Task<ProjectionRunResult> RetryBatchAsync(
        IEventProjection projection,
        int batchSize,
        CancellationToken cancellationToken) =>
        RunBatchAsync(projection, batchSize, cancellationToken);
}
