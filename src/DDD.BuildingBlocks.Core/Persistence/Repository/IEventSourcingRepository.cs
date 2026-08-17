using System;
using System.Threading.Tasks;
using System.Threading;
using DDD.BuildingBlocks.Core.Domain;

namespace DDD.BuildingBlocks.Core.Persistence.Repository
{
    public interface IEventSourcingRepository
    {
        Task<object?> GetByIdAsync(string id, Type type, long version, CancellationToken cancellationToken);
        Task<T?> GetByIdAsync<T, TKey>(TKey id, CancellationToken cancellationToken) where T : AggregateRoot<TKey> where TKey : EntityId<TKey>;
        Task SaveAsync(IEventSourcingBasedAggregate aggregate, CancellationToken cancellationToken);
    }
}
