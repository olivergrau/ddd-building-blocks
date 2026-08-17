using System.Threading;
using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Core.Commanding
{
    public interface ICommandDispatcher
    {
        Task<CommandExecutionResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
            where TCommand : class, ICommand;
    }
}
