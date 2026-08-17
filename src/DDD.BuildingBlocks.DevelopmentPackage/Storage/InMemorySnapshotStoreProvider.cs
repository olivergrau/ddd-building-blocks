using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;
using DDD.BuildingBlocks.Core.Persistence.Storage;

namespace DDD.BuildingBlocks.DevelopmentPackage.Storage;

public sealed class InMemorySnapshotStoreProvider : ISnapshotStoreProvider, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<StreamKey, SortedDictionary<long, SnapshotEnvelope>> _snapshots = [];

    public InMemorySnapshotStoreProvider(int snapshotFrequency)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(snapshotFrequency, 1);
        SnapshotFrequency = snapshotFrequency;
    }

    public int SnapshotFrequency { get; }

    public async Task<SnapshotEnvelope?> ReadAsync(
        string streamId,
        string aggregateType,
        long? maxStreamVersion,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        if (maxStreamVersion is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxStreamVersion));
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_snapshots.TryGetValue(new StreamKey(streamId, aggregateType), out var versions))
            {
                return null;
            }

            return versions.Values.LastOrDefault(item => maxStreamVersion is null || item.StreamVersion <= maxStreamVersion);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task WriteAsync(SnapshotEnvelope snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var key = new StreamKey(snapshot.StreamId, snapshot.AggregateType);
            if (!_snapshots.TryGetValue(key, out var versions))
            {
                versions = [];
                _snapshots.Add(key, versions);
            }

            versions[snapshot.StreamVersion] = snapshot;
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

    private readonly record struct StreamKey(string StreamId, string AggregateType);
}
