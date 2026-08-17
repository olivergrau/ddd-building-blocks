using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Domain;

namespace DDD.BuildingBlocks.Core.Commanding
{
    /// <summary>
    ///     Represents the concept of the sourcing of an aggregate.
    /// </summary>
    public interface IAggregateSourcing
    {
        Task<T> Source<T, TKey>(Command command, object[] constructorArguments, CancellationToken cancellationToken)
            where T : AggregateRoot<TKey>, new() where TKey : EntityId<TKey>;
    }
}
