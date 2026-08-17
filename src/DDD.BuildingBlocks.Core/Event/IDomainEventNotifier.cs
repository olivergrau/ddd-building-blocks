using System.Threading;
using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Core.Event
{
    public interface IDomainEventNotifier
    {
        Task NotifyAsync(IDomainEvent @event, CancellationToken cancellationToken);
    }
}
