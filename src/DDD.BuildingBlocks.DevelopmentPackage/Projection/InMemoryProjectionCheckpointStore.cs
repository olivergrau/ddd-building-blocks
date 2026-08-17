using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Projection;

namespace DDD.BuildingBlocks.DevelopmentPackage.Projection;

public sealed class InMemoryProjectionCheckpointStore : IProjectionCheckpointStore, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<ProjectionKey, ProjectionCheckpoint> _checkpoints = [];

    public async Task<ProjectionCheckpoint> GetAsync(ProjectionKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return GetOrCreate(key);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> ProcessAsync(
        ProjectionKey key,
        EventEnvelope envelope,
        Func<ProjectionTransactionContext, CancellationToken, Task> apply,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(apply);
        var position = envelope.GlobalPosition ?? throw new ArgumentException(
            "A projection requires a committed envelope.", nameof(envelope));

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var checkpoint = GetOrCreate(key);
            if (position <= checkpoint.LastProcessedPosition)
            {
                return false;
            }

            if (position != checkpoint.LastProcessedPosition + 1)
            {
                throw new InvalidOperationException(
                    $"Projection '{key.Name}' expected position {checkpoint.LastProcessedPosition + 1}, but received {position}.");
            }

            await apply(ProjectionTransactionContext.None, cancellationToken).ConfigureAwait(false);
            _checkpoints[key] = checkpoint with
            {
                LastProcessedPosition = position,
                Status = ProjectionStatus.Running,
                FailedPosition = null,
                LastError = null,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MarkFailedAsync(
        ProjectionKey key,
        long failedPosition,
        string error,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _checkpoints[key] = GetOrCreate(key) with
            {
                Status = ProjectionStatus.Faulted,
                FailedPosition = failedPosition,
                LastError = error,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ResetAsync(
        ProjectionKey key,
        Func<ProjectionTransactionContext, CancellationToken, Task> resetReadModel,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(resetReadModel);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await resetReadModel(ProjectionTransactionContext.None, cancellationToken).ConfigureAwait(false);
            _checkpoints[key] = new ProjectionCheckpoint(
                key, -1, ProjectionStatus.Rebuilding, null, null, DateTimeOffset.UtcNow);
        }
        finally
        {
            _gate.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _gate.Dispose();
        return ValueTask.CompletedTask;
    }

    private ProjectionCheckpoint GetOrCreate(ProjectionKey key)
    {
        if (_checkpoints.TryGetValue(key, out var checkpoint))
        {
            return checkpoint;
        }

        checkpoint = new ProjectionCheckpoint(key, -1, ProjectionStatus.Idle, null, null, DateTimeOffset.UtcNow);
        _checkpoints.Add(key, checkpoint);
        return checkpoint;
    }
}
