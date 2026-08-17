using DDD.BuildingBlocks.Core.Commanding;
using System.Threading;

namespace RocketLaunch.Application
{
    public interface IDomainEntry
    {
        Task<ICommandExecutionResult> ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
            where TCommand : DDD.BuildingBlocks.Core.Commanding.Command;
    }
}
