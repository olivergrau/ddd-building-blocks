using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Core.Event
{
    public interface ISubscribe<in T> where T : IDomainEvent
    {
        Task WhenAsync(T @event);
    }
}
