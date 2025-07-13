using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.ErrorHandling;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Application.Command;
using RocketLaunch.Application.Command.CrewMember.Handler;
using RocketLaunch.Application.Command.Mission.Handler;
using RocketLaunch.Domain.Service;

namespace RocketLaunch.Application
{
    public class DomainEntry : IDomainEntry
    {
        private readonly ICommandProcessor _commandProcessor;
        private readonly IResourceAvailabilityService _validator;
        private readonly CrewAssignment _crewAssignment;
        private readonly CrewUnassignment _crewUnassignment;

        public DomainEntry(
            ICommandProcessor commandProcessor, IEventSourcingRepository repository, IResourceAvailabilityService validator)
        {
            _commandProcessor = commandProcessor;
            _validator = validator;
            _crewAssignment = new CrewAssignment(validator);
            _crewUnassignment = new CrewUnassignment();

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
            _commandProcessor.RegisterHandlerFactory(() => new AbortMissionCommandHandler(repository, _crewUnassignment));
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
            var result = await _commandProcessor.ExecuteAsync(command);

            if (result.IsSuccess || result.ResultException == null)
            {
                return result;
            }

            if (result.ResultException is ClassifiedErrorException ce)
            {
                return new CommandExecutionResult(false, ce.ErrorInfo.Message, ce);
            }

            var wrapped = result.ResultException switch
            {
                NotFoundException nf => new ClassifiedErrorException(
                    new ClassificationInfo(nf.Message, ErrorOrigin.ApplicationLevel, ErrorClassification.NotFound), nf),
                _ => new ApplicationProcessingException(HandlerErrors.ApplicationProcessingError, result.ResultException)
            };

            return new CommandExecutionResult(false, wrapped.ErrorInfo.Message, wrapped);
        }
    }
}
