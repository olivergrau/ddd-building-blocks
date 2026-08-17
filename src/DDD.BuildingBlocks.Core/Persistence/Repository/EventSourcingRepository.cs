using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Event.Serialization;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Extension;
using DDD.BuildingBlocks.Core.Persistence.Storage;
using DDD.BuildingBlocks.Core.Util;

// ReSharper disable SuspiciousTypeConversion.Global

namespace DDD.BuildingBlocks.Core.Persistence.Repository
{
    using SnapshotSupport;

    /// <inheritdoc />
    /// <summary>
    ///     A repository implementation which uses the concept of event sourcing for persisting the data. The underlying
    ///     interfaces
    ///     utilizes a more universal approach which allows the repository to handle different types of aggregates.
    /// </summary>
    /// <remarks>
    ///     The actual data handling is the responsibility of the IEventStoreProvider and ISnapshotStoreProvider
    ///     implementations.
    ///     The repository orchestrates the storage providers and is only responsible for persisting the events.
    ///     Keep also in mind that there is only a need for one event repository if you use event sourcing as a storage
    ///     mechanism.
    ///     It persists all events regardless from which aggregate type the event came.
    ///     You can decorate the EventSourcingRepository if you wish and add so aggregate specific functionality (for cross
    ///     cutting concerns like logging) to it.
    ///     Keep in mind that the repository is NOT responsible for publishing the events.
    /// </remarks>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class EventSourcingRepository(
        IEventStoreProvider eventStoreProvider,
        IEventCodec eventCodec,
        ISnapshotStoreProvider? snapshotStoreProvider = null,
        ISnapshotCodec? snapshotCodec = null)
        : IEventSourcingRepository
    {
        private readonly bool _snapshotConfigurationIsValid =
            (snapshotStoreProvider is null) == (snapshotCodec is null)
                ? true
                : throw new ArgumentException(
                    "Snapshot store and snapshot codec must either both be configured or both be omitted.");
        private readonly IEventStoreProvider _eventStoreProvider = eventStoreProvider ?? throw new ArgumentNullException(nameof(eventStoreProvider));
        private readonly IEventCodec _eventCodec = eventCodec ?? throw new ArgumentNullException(nameof(eventCodec));

        private bool HasSnapshotSupport => _snapshotConfigurationIsValid && snapshotStoreProvider is not null;

        public virtual async Task<object?> GetByIdAsync(string id, Type type, long version, System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            object? item = default;

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(nameof(id));
            }

            var isSnapshotEnabled = typeof(ISnapshotEnabled).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
            Snapshot? snapshot = null;

            if (isSnapshotEnabled && HasSnapshotSupport)
            {
                var envelope = await snapshotStoreProvider!.ReadAsync(
                    id, GetAggregateType(type), version >= 0 ? version : null, cancellationToken);
                snapshot = envelope is null ? null : snapshotCodec!.Decode(envelope);
            }

            if (snapshot != null)
            {
                item = Activator.CreateInstance(type) ?? throw new InvalidOperationException();
                ((ISnapshotEnabled) item).ApplySnapshot(snapshot);

                if (version < 0 || ((IEventSourcingBasedAggregate)item).CurrentVersion < version)
                {
                    var events = await ReadEventsAsync(type, id, snapshot.Version + 1, cancellationToken);
                    ((IEventSourcingBasedAggregate)item).ReplayEvents(events);
                }
            }
            else
            {
                var events = (await ReadEventsAsync(type, id, 0, cancellationToken))
                    .Take(version >= 0 ? checked((int)Math.Min(version + 1, int.MaxValue)) : int.MaxValue)
                    .ToList();

                if (events.Count != 0)
                {
                    item = Activator.CreateInstance(type) ?? throw new InvalidOperationException();
                    ((IEventSourcingBasedAggregate)item).ReplayEvents(events);
                }
            }

            return item;
        }

        public virtual async Task<T?> GetByIdAsync<T, TKey>(TKey id, System.Threading.CancellationToken cancellationToken)
            where T : AggregateRoot<TKey> where TKey : EntityId<TKey>
        {
            cancellationToken.ThrowIfCancellationRequested();
            T? item = default;

            if (string.IsNullOrWhiteSpace(id.ToString()))
            {
                throw new ArgumentException(nameof(id));
            }

            var isSnapshotEnabled = typeof(ISnapshotEnabled).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo());
            Snapshot? snapshot = null;

            if (isSnapshotEnabled && HasSnapshotSupport)
            {
                var envelope = await snapshotStoreProvider!.ReadAsync(
                    id.ToString() ?? throw new InvalidOperationException(), GetAggregateType(typeof(T)), null, cancellationToken);
                snapshot = envelope is null ? null : snapshotCodec!.Decode(envelope);
            }

            if (snapshot != null)
            {
                item = ReflectionHelper.CreateInstance<T, TKey>();
                ((ISnapshotEnabled) item).ApplySnapshot(snapshot);
                var events = await ReadEventsAsync(
                    typeof(T), id.ToString() ?? throw new InvalidOperationException(), snapshot.Version + 1, cancellationToken);
                item.ReplayEvents(events);
            }
            else
            {
                var events = (await ReadEventsAsync(
                    typeof(T), id.ToString() ?? throw new InvalidOperationException(), 0, cancellationToken)).ToList();

                if (events.Count != 0)
                {
                    item = ReflectionHelper.CreateInstance<T, TKey>();
                    item.ReplayEvents(events);
                }
            }

            return item;
        }

        public virtual async Task SaveAsync(IEventSourcingBasedAggregate aggregate, System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (aggregate.HasUncommittedChanges())
            {
                await CommitChanges(aggregate, cancellationToken);
            }
        }

        private async Task CommitChanges(IEventSourcingBasedAggregate aggregate, System.Threading.CancellationToken cancellationToken)
        {
            var expectedVersion = aggregate.LastCommittedVersion;

            var changesToCommit = aggregate.GetUncommittedChanges().ToList();

            // perform pre commit actions
            foreach (var e in changesToCommit)
            {
                DoPreCommitTasks(e);
            }

            var aggregateType = GetAggregateType(aggregate.GetType());
            var envelopes = changesToCommit.Select(@event => _eventCodec.Encode(
                @event,
                new EventEnvelopeMetadata(
                    Guid.NewGuid(),
                    aggregate.SerializedId,
                    aggregateType,
                    @event.TargetVersion + 1,
                    null,
                    new DateTimeOffset(@event.EventCommittedTimestamp, TimeSpan.Zero),
                    null,
                    @event.CorrelationId))).ToArray();

            await _eventStoreProvider.AppendAsync(
                aggregate.SerializedId,
                aggregateType,
                expectedVersion,
                envelopes,
                cancellationToken);

            aggregate.MarkChangesAsCommitted();

            // If the Aggregate implements SnapshotEnabled
            if (aggregate is ISnapshotEnabled snapshotEnabled && HasSnapshotSupport)
            {
                if (aggregate.CurrentVersion >= snapshotStoreProvider!.SnapshotFrequency &&
                    (
                        changesToCommit.Count >=
                        snapshotStoreProvider.SnapshotFrequency || // more events at once than {snapshot value}
                        aggregate.CurrentVersion % snapshotStoreProvider.SnapshotFrequency < changesToCommit.Count ||
                        aggregate.CurrentVersion % snapshotStoreProvider.SnapshotFrequency == 0 // every {snapshot value} elements
                    )
                   )
                {
                    try
                    {
                        var snapshot = snapshotEnabled.TakeSnapshot() ?? throw new InvalidOperationException();
                        await snapshotStoreProvider.WriteAsync(
                            snapshotCodec!.Encode(snapshot, aggregateType), System.Threading.CancellationToken.None);
                    }
                    catch (System.Exception)
                    {
                        // Snapshot persistence is a disposable optimization and never changes event-commit success.
                    }
                }
            }
        }

        private static void DoPreCommitTasks(IDomainEvent e)
        {
            e.EventCommittedTimestamp = ApplicationTime.Current;
        }

        private async Task<IReadOnlyList<IDomainEvent>> ReadEventsAsync(
            Type aggregateType,
            string streamId,
            long fromStreamVersion,
            System.Threading.CancellationToken cancellationToken)
        {
            var envelopes = await _eventStoreProvider.ReadStreamAsync(
                streamId,
                GetAggregateType(aggregateType),
                fromStreamVersion,
                int.MaxValue,
                cancellationToken);
            return envelopes.Select(_eventCodec.Decode).ToArray();
        }

        private static string GetAggregateType(Type aggregateType) =>
            aggregateType.FullName ?? throw new InvalidOperationException("Aggregate type has no stable full name.");
    }
}
