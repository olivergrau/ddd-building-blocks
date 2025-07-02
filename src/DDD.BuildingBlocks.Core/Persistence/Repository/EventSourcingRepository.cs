using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Event;
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
    ///     The actual data handling is the responsibility of the IEventStorageProvider and ISnapshotStorageProvider
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
    public class EventSourcingRepository(IEventStorageProvider eventStorageProvider, ISnapshotStorageProvider? snapshotStorageProvider = null)
        : IEventSourcingRepository
    {
        private readonly IEventStorageProvider _eventStorageProvider = eventStorageProvider ?? throw new ArgumentNullException(nameof(eventStorageProvider));

        public virtual async Task<object> GetByIdAsync(string id, Type type, int version = -1)
        {
            object item = default!;

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(nameof(id));
            }

            var isSnapshotEnabled = typeof(ISnapshotEnabled).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
            Snapshot? snapshot = null;

            if (isSnapshotEnabled && snapshotStorageProvider != null)
            {
                if(version >= 0)
                {
                    snapshot = await snapshotStorageProvider.GetSnapshotAsync(id, version);
                }
                else
                {
                    snapshot = await snapshotStorageProvider.GetSnapshotAsync(id);
                }
            }

            if (snapshot != null)
            {
                item = Activator.CreateInstance(type) ?? throw new InvalidOperationException();
                ((ISnapshotEnabled) item).ApplySnapshot(snapshot);

                if (version < 0 || ((IEventSourcingBasedAggregate)item).CurrentVersion < version)
                {
                    var events =
                        await _eventStorageProvider.GetEventsAsync(type, id, snapshot.Version + 1, int.MaxValue);

                    if(events != null)
                    {
                        ((IEventSourcingBasedAggregate)item).ReplayEvents(events);
                    }
                }
            }
            else
            {
                var eventsList = await _eventStorageProvider.GetEventsAsync(type, id, 0, version >= 0 ? version + 1 : int.MaxValue);

                if (eventsList == null)
                {
                    return null;
                }

                var events = eventsList.ToList();

                if (events.Count != 0)
                {
                    item = Activator.CreateInstance(type) ?? throw new InvalidOperationException();
                    ((IEventSourcingBasedAggregate)item).ReplayEvents(events);
                }
            }

            return item;
        }

        public virtual async Task<T?> GetByIdAsync<T, TKey>(TKey id)
            where T : AggregateRoot<TKey> where TKey : EntityId<TKey>
        {
            T item = default!;

            if (string.IsNullOrWhiteSpace(id.ToString()))
            {
                throw new ArgumentException(nameof(id));
            }

            var isSnapshotEnabled = typeof(ISnapshotEnabled).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo());
            Snapshot? snapshot = null;

            if (isSnapshotEnabled && snapshotStorageProvider != null)
            {
                snapshot = await snapshotStorageProvider.GetSnapshotAsync(id.ToString()!);
            }

            if (snapshot != null)
            {
                item = ReflectionHelper.CreateInstance<T, TKey>();
                ((ISnapshotEnabled) item).ApplySnapshot(snapshot);
                var events =
                    await _eventStorageProvider.GetEventsAsync(typeof(T), id.ToString()!, snapshot.Version + 1, int.MaxValue);

                if(events != null)
                {
                    item.ReplayEvents(events);
                }
            }
            else
            {
                var eventsList = await _eventStorageProvider.GetEventsAsync(typeof(T), id.ToString()!, 0, int.MaxValue);

                if (eventsList == null)
                {
                    return null;
                }

                var events = eventsList.ToList();

                if (events.Count != 0)
                {
                    item = ReflectionHelper.CreateInstance<T, TKey>();
                    item.ReplayEvents(events);
                }
            }

            return item;
        }

        public virtual async Task SaveAsync(IEventSourcingBasedAggregate aggregate)
        {
            if (aggregate.HasUncommittedChanges())
            {
                await CommitChanges(aggregate);
            }
        }

        private async Task CommitChanges(IEventSourcingBasedAggregate aggregate)
        {
            var expectedVersion = aggregate.LastCommittedVersion;

            var item = await _eventStorageProvider.GetLastEventAsync(aggregate.GetType(), aggregate.SerializedId);

            if (item != null && expectedVersion == (int) StreamState.NoStream)
            {
                throw new AggregateCreationException(
                    $"Aggregate {item.CorrelationId} can't be created as it already exists with version {item.TargetVersion + 1}");
            }

            if (item != null && item.TargetVersion + 1 != expectedVersion)
            {
                throw new ConcurrencyException(
                    $"Aggregate {item.CorrelationId} has been modified externally and has an updated state. Can't commit changes.");
            }

            var changesToCommit = aggregate.GetUncommittedChanges().ToList();

            // perform pre commit actions
            foreach (var e in changesToCommit)
            {
                DoPreCommitTasks(e);
            }

            // CommitAsync events to storage provider
            await _eventStorageProvider.CommitChangesAsync(aggregate);

            // If the Aggregate implements SnapshotEnabled
            if (aggregate is ISnapshotEnabled snapshotEnabled && snapshotStorageProvider != null)
            {
                if (aggregate.CurrentVersion >= snapshotStorageProvider.SnapshotFrequency &&
                    (
                        changesToCommit.Count >=
                        snapshotStorageProvider.SnapshotFrequency || // more events at once than {snapshot value}
                        aggregate.CurrentVersion % snapshotStorageProvider.SnapshotFrequency < changesToCommit.Count ||
                        aggregate.CurrentVersion % snapshotStorageProvider.SnapshotFrequency == 0 // every {snapshot value} elements
                    )
                   )
                {
                    await snapshotStorageProvider.SaveSnapshotAsync(snapshotEnabled.TakeSnapshot());
                }
            }

            aggregate.MarkChangesAsCommitted();
        }

        private static void DoPreCommitTasks(IDomainEvent e)
        {
            e.EventCommittedTimestamp = ApplicationTime.Current;
        }
    }
}
