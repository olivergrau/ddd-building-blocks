using System;
using DDD.BuildingBlocks.Core.Message;

namespace DDD.BuildingBlocks.Core.Event
{
    public interface IDomainEvent : IMessage
    {
        /// <summary>
        /// Target version of the Aggregate this event will be applied against
        /// </summary>
        int TargetVersion { get; set; }
        
        /// <summary>
        /// The aggregateID of the aggregate
        /// </summary>
        string? SerializedAggregateId { get; set; }

        /// <summary>
        /// This is used to timestamp the event when it get's committed
        /// </summary>
        DateTime EventCommittedTimestamp { get; set; }

        /// <summary>
        /// This is used to handle versioning of events over time when refactoring or feature additions are done
        /// </summary>
        int ClassVersion { get; set; }

        /// <summary>
        ///     The type info for the event.
        /// </summary>
        string FullType { get; }
    }
}
