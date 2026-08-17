using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Persistence.Storage;

namespace DDD.BuildingBlocks.DevelopmentPackage.Storage;

public sealed class InMemoryEventStoreProvider : IEventStoreProvider, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<StreamKey, List<EventEnvelope>> _streams = [];
    private readonly List<EventEnvelope> _committedFeed = [];
    private readonly HashSet<Guid> _eventIds = [];
    private long _nextGlobalPosition;

    public async Task<AppendEventsResult> AppendAsync(
        string streamId,
        string aggregateType,
        long expectedVersion,
        IReadOnlyCollection<EventEnvelope> events,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentOutOfRangeException.ThrowIfLessThan(expectedVersion, -1);

        if (events.Count == 0)
        {
            throw new ArgumentException("At least one event is required.", nameof(events));
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = new StreamKey(streamId, aggregateType);
            var actualVersion = _streams.TryGetValue(key, out var stream) ? stream.Count - 1L : -1L;

            if (actualVersion != expectedVersion)
            {
                if (expectedVersion == -1)
                {
                    throw new EventStreamAlreadyExistsException(streamId, actualVersion);
                }

                if (actualVersion == -1)
                {
                    throw new EventStreamNotFoundException(streamId);
                }

                throw new EventStoreConcurrencyException(streamId, expectedVersion, actualVersion);
            }

            ValidateBatch(streamId, aggregateType, expectedVersion, events);

            var committedAt = DateTimeOffset.UtcNow;
            var firstGlobalPosition = _nextGlobalPosition;
            var committed = events.Select(source => CopyForCommit(source, _nextGlobalPosition++, committedAt)).ToArray();

            if (stream is null)
            {
                stream = [];
                _streams.Add(key, stream);
            }

            stream.AddRange(committed);
            _committedFeed.AddRange(committed);
            foreach (var envelope in committed)
            {
                _eventIds.Add(envelope.EventId);
            }

            return new AppendEventsResult(
                stream.Count - 1L,
                firstGlobalPosition,
                _nextGlobalPosition - 1L);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<EventEnvelope>> ReadStreamAsync(
        string streamId,
        string aggregateType,
        long fromStreamVersion,
        int maxCount,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentOutOfRangeException.ThrowIfNegative(fromStreamVersion);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, 1);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _streams.TryGetValue(new StreamKey(streamId, aggregateType), out var stream)
                ? stream.Where(item => item.StreamVersion >= fromStreamVersion).Take(maxCount).ToArray()
                : [];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<EventEnvelope>> ReadCommittedFeedAsync(
        long afterGlobalPosition,
        int maxCount,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(afterGlobalPosition, -1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, 1);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _committedFeed
                .Where(item => item.GlobalPosition > afterGlobalPosition)
                .Take(maxCount)
                .ToArray();
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

    private void ValidateBatch(
        string streamId,
        string aggregateType,
        long expectedVersion,
        IReadOnlyCollection<EventEnvelope> events)
    {
        var batchIds = new HashSet<Guid>();
        var nextVersion = expectedVersion + 1L;

        foreach (var envelope in events)
        {
            if (!string.Equals(envelope.StreamId, streamId, StringComparison.Ordinal) ||
                !string.Equals(envelope.AggregateType, aggregateType, StringComparison.Ordinal))
            {
                throw new ArgumentException("Every envelope must target the appended stream and aggregate type.", nameof(events));
            }

            if (envelope.StreamVersion != nextVersion++)
            {
                throw new ArgumentException("Envelope stream versions must be contiguous after the expected version.", nameof(events));
            }

            if (envelope.GlobalPosition is not null || envelope.CommittedAt is not null)
            {
                throw new ArgumentException("Global position and commit timestamp are assigned by the provider.", nameof(events));
            }

            if (!batchIds.Add(envelope.EventId) || _eventIds.Contains(envelope.EventId))
            {
                throw new DuplicateEventIdException(envelope.EventId);
            }
        }
    }

    private static EventEnvelope CopyForCommit(
        EventEnvelope source,
        long globalPosition,
        DateTimeOffset committedAt)
    {
        return new EventEnvelope(
            source.EventId,
            source.StreamId,
            source.AggregateType,
            source.StreamVersion,
            globalPosition,
            source.EventType,
            source.SchemaVersion,
            source.OccurredAt,
            committedAt,
            source.CorrelationId,
            source.CausationId,
            source.CommandId,
            source.Actor,
            source.TurnId,
            source.Payload);
    }

    private readonly record struct StreamKey(string StreamId, string AggregateType);
}
