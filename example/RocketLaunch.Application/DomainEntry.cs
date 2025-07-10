using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Application.Command;
using RocketLaunch.Application.Command.Handler;
using RocketLaunch.Application.Command.Mission.Handler;
using RocketLaunch.Domain.Service;

namespace RocketLaunch.Application
{
    public class DomainEntry : IDomainEntry
    {
        private readonly ICommandProcessor _commandProcessor;
        private readonly IResourceAvailabilityService _validator;
        private readonly CrewAssignment _crewAssignment;

        public DomainEntry(
            ICommandProcessor commandProcessor, IEventSourcingRepository repository, IResourceAvailabilityService validator)
        {
            _commandProcessor = commandProcessor;
            _validator = validator;
            _crewAssignment = new CrewAssignment(validator);

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
            _commandProcessor.RegisterHandlerFactory(() => new AssignRocketCommandHandler(repository, _validator));
            _commandProcessor.RegisterHandlerFactory(
                () => new AssignLaunchPadCommandHandler(repository, _validator));
            _commandProcessor.RegisterHandlerFactory(
                () => new AssignCrewCommandHandler(repository, _crewAssignment));
            _commandProcessor.RegisterHandlerFactory(() => new ScheduleMissionCommandHandler(repository));
            _commandProcessor.RegisterHandlerFactory(() => new LaunchMissionCommandHandler(repository));
            _commandProcessor.RegisterHandlerFactory(() => new AbortMissionCommandHandler(repository));
            _commandProcessor.RegisterHandlerFactory(() => new MarkMissionArrivedCommandHandler(repository));

            _commandProcessor.RegisterHandlerFactory(() => new RegisterCrewMemberCommandHandler(repository));
            _commandProcessor.RegisterHandlerFactory(() => new AssignCrewMemberCommandHandler(repository));
            _commandProcessor.RegisterHandlerFactory(() => new ReleaseCrewMemberCommandHandler(repository));
            _commandProcessor.RegisterHandlerFactory(() => new SetCrewMemberCertificationsCommandHandler(repository));
            _commandProcessor.RegisterHandlerFactory(() => new SetCrewMemberStatusCommandHandler(repository));
        }

        public async Task<ICommandExecutionResult> ExecuteAsync<TCommand>(TCommand command)
            where TCommand : DDD.BuildingBlocks.Core.Commanding.Command
        {
            return await _commandProcessor.ExecuteAsync(command);
        }
    }
}
