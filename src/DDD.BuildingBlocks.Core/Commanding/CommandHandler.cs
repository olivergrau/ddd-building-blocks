using System;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Persistence.Repository;

namespace DDD.BuildingBlocks.Core.Commanding
{
    public abstract class CommandHandler<T>(IEventSourcingRepository eventSourcingRepository) : ICommandHandler<T>
        where T : ICommand
    {
        protected AggregateSourcing AggregateSourcing { get; } = new(eventSourcingRepository);

        protected IEventSourcingRepository AggregateRepository { get; } = eventSourcingRepository ?? throw new ArgumentNullException(nameof(eventSourcingRepository));

        public abstract Task HandleCommandAsync(T command);
    }
}
