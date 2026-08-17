using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;
using Microsoft.Extensions.DependencyInjection;

namespace DDD.BuildingBlocks.DI.Extensions.Dispatching;

public sealed class ScopedDomainEventNotifier(IServiceScopeFactory scopeFactory) : IDomainEventNotifier
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    public Task NotifyAsync(IDomainEvent @event, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return NotifyTypedAsync((dynamic)@event, cancellationToken);
    }

    private async Task NotifyTypedAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var subscribers = scope.ServiceProvider.GetServices<ISubscribe<TEvent>>().ToArray();

        foreach (var subscriber in subscribers)
        {
            await subscriber.HandleAsync(@event, cancellationToken);
        }
    }
}
