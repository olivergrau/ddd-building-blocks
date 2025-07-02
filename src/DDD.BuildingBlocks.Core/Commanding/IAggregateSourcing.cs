using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Domain;

namespace DDD.BuildingBlocks.Core.Commanding
{
    /// <summary>
    ///     Represents the concept of the sourcing of an aggregate.
    /// </summary>
    public interface IAggregateSourcing
    {
        Task<T> Source<T, TKey>(Command command, params object[] p)
            where T : AggregateRoot<TKey>, new() where TKey : EntityId<TKey>;
    }
}