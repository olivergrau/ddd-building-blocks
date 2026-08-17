using System;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Core.Projection;

public interface IProjectionCheckpointStore
{
    Task<ProjectionCheckpoint> GetAsync(ProjectionKey key, CancellationToken cancellationToken);

    Task<bool> ProcessAsync(
        ProjectionKey key,
        EventEnvelope envelope,
        Func<ProjectionTransactionContext, CancellationToken, Task> apply,
        CancellationToken cancellationToken);

    Task MarkFailedAsync(
        ProjectionKey key,
        long failedPosition,
        string error,
        CancellationToken cancellationToken);

    Task ResetAsync(
        ProjectionKey key,
        Func<ProjectionTransactionContext, CancellationToken, Task> resetReadModel,
        CancellationToken cancellationToken);
}
