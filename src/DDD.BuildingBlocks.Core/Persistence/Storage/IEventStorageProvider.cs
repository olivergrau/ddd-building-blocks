using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Core.Persistence.Storage
{
    public interface IEventStorageProvider
    {
        Task<IEnumerable<IDomainEvent>?> GetEventsAsync(Type aggregateType, string key, int start, int count);

        Task<IDomainEvent?> GetLastEventAsync(Type aggregateType, string key);

        Task CommitChangesAsync(IEventSourcingBasedAggregate aggregate);
    }
}
