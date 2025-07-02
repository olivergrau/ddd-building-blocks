using System;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Domain;

namespace DDD.BuildingBlocks.Core.Persistence.Repository
{
    public interface IEventSourcingRepository
    {
        Task<object?> GetByIdAsync(string id, Type type, int version = -1);
        Task<T?> GetByIdAsync<T, TKey>(TKey id) where T : AggregateRoot<TKey> where TKey : EntityId<TKey>;
        Task SaveAsync(IEventSourcingBasedAggregate aggregate);
    }
}
