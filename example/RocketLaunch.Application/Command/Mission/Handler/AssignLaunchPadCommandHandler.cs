using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Domain.Model.Entities;
using RocketLaunch.Domain.Service;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Application.Command.Mission.Handler;

public class AssignLaunchPadCommandHandler(IEventSourcingRepository repository, IResourceAvailabilityService validator)
    : CommandHandler<AssignLaunchPadCommand>(repository)
{
    public override async Task HandleCommandAsync(AssignLaunchPadCommand command)
    {
        Domain.Model.Mission mission;

        try
        {            
            mission = await AggregateSourcing.Source<Domain.Model.Mission, MissionId>(command);
        }
        catch (Exception e)
        {
            throw new ApplicationProcessingException(HandlerErrors.ApplicationProcessingError, e);
        }

        await mission.AssignLaunchPadAsync(
            new LaunchPad(
                new LaunchPadId(command.LaunchPadId), command.Name, command.Location, command.SupportedRockets), validator);

        await AggregateRepository.SaveAsync(mission);
    }
}
