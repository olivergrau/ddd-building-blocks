using System;

namespace DDD.BuildingBlocks.Core.Event
{
    public abstract class DomainEvent : IDomainEvent
    {
        /// <summary>
        ///     The TargetVersion of the aggregate. If not match, changes won't be applied.
        /// </summary>
        public int TargetVersion { get; set; }

        /// <summary>
        ///     Aggregate Id in serialized representation.
        /// </summary>
        public string? SerializedAggregateId { get;  set; }

        /// <summary>
        ///     An id which represents a context around a series of events.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        ///     Timestamp of the event.
        /// </summary>
        public DateTime EventCommittedTimestamp { get; set; }

        /// <summary>
        ///     Represents the code version of the event.
        /// </summary>
        public int ClassVersion { get; set; }

        public string FullType => GetType().AssemblyQualifiedName!;

        protected DomainEvent()
        {
        }

        protected DomainEvent(string? serializedAggregateId, int targetVersion, int eventClassVersion)
        {
            if (string.IsNullOrWhiteSpace(serializedAggregateId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serializedAggregateId));
            }

            SerializedAggregateId = serializedAggregateId;
            TargetVersion = targetVersion;
            ClassVersion = eventClassVersion;
            CorrelationId = CorrelatedScope.Current;
        }
    }
}
