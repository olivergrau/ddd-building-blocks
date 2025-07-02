using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Core.Event
{
    public interface IDomainEventHandler
    {
        Task HandleAsync(IDomainEvent @event);
    }
}
