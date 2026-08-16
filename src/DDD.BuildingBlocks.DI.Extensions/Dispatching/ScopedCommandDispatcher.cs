using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core;
using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using Microsoft.Extensions.DependencyInjection;

namespace DDD.BuildingBlocks.DI.Extensions.Dispatching;

public sealed class ScopedCommandDispatcher(IServiceScopeFactory scopeFactory) : ICommandDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    public async Task<CommandExecutionResult> DispatchAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken)
        where TCommand : class, ICommand
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        await using var scope = _scopeFactory.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices<ICommandHandler<TCommand>>().ToArray();

        if (handlers.Length != 1)
        {
            throw new HandlerRegistrationException(
                $"Command {typeof(TCommand).FullName} requires exactly one handler, but {handlers.Length} were resolved.");
        }

        try
        {
            await handlers[0].HandleAsync(command, cancellationToken);
            return new CommandExecutionResult(true, string.Empty, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (System.Exception exception)
        {
            return new CommandExecutionResult(false, CoreErrors.CommandExecutionFailed, exception);
        }
    }
}
