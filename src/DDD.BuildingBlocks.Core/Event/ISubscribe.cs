using System.Threading;
using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Core.Event
{
    public interface ISubscribe<in T> where T : IDomainEvent
    {
        Task HandleAsync(T @event, CancellationToken cancellationToken);
    }
}
