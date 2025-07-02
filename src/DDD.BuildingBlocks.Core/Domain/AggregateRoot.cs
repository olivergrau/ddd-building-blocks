using System;
using System.Collections.Generic;
using System.Linq;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Extension;
using DDD.BuildingBlocks.Core.Persistence;
using AggregateException = DDD.BuildingBlocks.Core.Exception.AggregateException;

namespace DDD.BuildingBlocks.Core.Domain
{
    /// <summary>
    ///     Represents an aggregate root which is the only one access point to your object graph.
    /// </summary>
    /// <typeparam name="TKey">The logical aggregate identifier.</typeparam>
    public abstract class AggregateRoot<TKey> : Entity<TKey>, IEventSourcingBasedAggregate where TKey : EntityId<TKey>
    {
        /// <summary>
        ///     Returns a reconstituted aggregate identifier based on TKey.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected abstract EntityId<TKey> GetIdFromStringRepresentation(string value);

        protected bool Deactivated { get; set; }

        private readonly List<IDomainEvent> _uncommittedChanges;
        private Dictionary<Type, string> _eventHandlerCache = null!;

        protected IEnumerable<string> CorrelationIds => _correlationIds.AsReadOnly();
        private readonly List<string> _correlationIds = [];

        public int CurrentVersion { get; protected set; }

        public int LastCommittedVersion { get; protected set; }

        public StreamState GetStreamState()
        {
            if (Deactivated)
            {
                return StreamState.StreamClosed;
            }

            return CurrentVersion == -1 ? StreamState.NoStream : StreamState.HasStream;
        }

        protected AggregateRoot(TKey id) : base(id)
        {
            CurrentVersion = (int)StreamState.NoStream;
            LastCommittedVersion = (int)StreamState.NoStream;
            _uncommittedChanges = [];
            PrepareInternalEventHandlers();
        }

        public bool HasUncommittedChanges()
        {
            lock (_uncommittedChanges)
            {
                return _uncommittedChanges.Count != 0;
            }
        }

        public IEnumerable<IDomainEvent> GetUncommittedChanges()
        {
            lock (_uncommittedChanges)
            {
                return _uncommittedChanges.ToList();
            }
        }

        public void MarkChangesAsCommitted()
        {
            lock (_uncommittedChanges)
            {
                _uncommittedChanges.Clear();
                LastCommittedVersion = CurrentVersion;
            }
        }

        public void ReplayEvents(IEnumerable<IDomainEvent> history)
        {
            foreach (var e in history)
            {
                //We call ApplyEvent with isNew parameter set to false as we are replaying a historical event
                ApplyEvent(e, false);
            }
            LastCommittedVersion = CurrentVersion;
        }

        /// <summary>
        /// This is used to handle new events
        /// </summary>
        /// <param name="event"></param>
        protected void ApplyEvent(IDomainEvent @event)
        {
            ApplyEvent(@event, true);
        }

        /// <summary>
        /// Finds the correct Apply() method in the AggregateRoot and call it to apply changes to aggregates state
        /// </summary>
        /// <param name="event">The event to handle</param>
        /// <param name="isNew">Is this a new DomainEvent</param>
        private void ApplyEvent(IDomainEvent @event, bool isNew)
        {
            if (Deactivated)
            {
                throw new AggregateException(@event.SerializedAggregateId, "Aggregate has been marked as deactivated.");
            }

            if (CanBeApplied(@event))
            {
                Apply(@event);

                if (!isNew)
                {
                    if (@event.CorrelationId != null && _correlationIds.All(q => q != @event.CorrelationId))
                    {
                        _correlationIds.Add(@event.CorrelationId);
                    }
                    return;
                }

                lock (_uncommittedChanges)
                {
                    _uncommittedChanges.Add(@event);
                }
            }
            else
            {
                throw new AggregateStateMismatchException(
                    $"The event target version is [{@event.SerializedAggregateId}].(Version {@event.TargetVersion}) and " +
                    $"AggregateRoot version is [{Id}].(Version {CurrentVersion})");
            }
        }

        private bool CanBeApplied(IDomainEvent @event)
        {
            if (Deactivated)
            {
                return false;
            }

            return (GetStreamState() == StreamState.NoStream || Id.ToString() == @event.SerializedAggregateId) && CurrentVersion == @event.TargetVersion;
        }

        private void Apply(IDomainEvent @event)
        {
            if (GetStreamState() == StreamState.NoStream)
            {
                //This is only needed for the very first event as every other event CAN ONLY apply to a matching ID
                Id = GetIdFromStringRepresentation(@event.SerializedAggregateId!) as TKey
                     ?? throw new AggregateCreationException("Cannot determine ID value from string representation. Aggregate could not be created.");
            }

            if (_eventHandlerCache.ContainsKey(@event.GetType()))
            {
                @event.InvokeOnAggregate(this, _eventHandlerCache[@event.GetType()]);
            }
            else
            {
                throw new AggregateEventOnApplyMethodMissingException($"No event handler specified for {@event.GetType()} on {GetType()}");
            }

            CurrentVersion++;
        }

        private void PrepareInternalEventHandlers()
        {
            _eventHandlerCache = ReflectionHelper.FindEventHandlerMethodsInAggregate(GetType());
        }
    }
}
