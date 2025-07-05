using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Application.Command;
using RocketLaunch.Application.Command.Handler;

namespace RocketLaunch.Application
{
    public class DomainEntry : IDomainEntry
    {
        private readonly ICommandProcessor _commandProcessor;

        public DomainEntry(
            ICommandProcessor commandProcessor, IEventSourcingRepository repository)
        {
            _commandProcessor = commandProcessor;

            RegisterCommandsForManagementDomain(repository);

            _commandProcessor.OnError += (_, args) =>
            {
                if(args.Exception != null)
                {
                    // Log the exception or handle it as needed
                    // For example, you could log it to a logging service
                    Console.WriteLine($"Error executing command: {args.Exception.Message}");
                }
            };
        }

        private void RegisterCommandsForManagementDomain(IEventSourcingRepository repository)
        {
            _commandProcessor.RegisterHandlerFactory(() => new RegisterMissionCommandHandler(repository));
        }

        public async Task<ICommandExecutionResult> ExecuteAsync<TCommand>(TCommand command)
            where TCommand : DDD.BuildingBlocks.Core.Commanding.Command
        {
            return await _commandProcessor.ExecuteAsync(command);
        }
    }
}
