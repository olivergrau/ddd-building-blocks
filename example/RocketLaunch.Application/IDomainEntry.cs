using DDD.BuildingBlocks.Core.Commanding;

namespace RocketLaunch.Application
{
    public interface IDomainEntry
    {
        Task<ICommandExecutionResult> ExecuteAsync<TCommand>(TCommand command)
            where TCommand : DDD.BuildingBlocks.Core.Commanding.Command;
    }
}