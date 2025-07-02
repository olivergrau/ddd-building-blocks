using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Core.Event
{
    public interface IDomainEventNotifier
    {
        Task NotifyAsync(IDomainEvent @event);
        
        void SetDependencyResolver(IDependencyResolver dependencyResolver);
    }
}