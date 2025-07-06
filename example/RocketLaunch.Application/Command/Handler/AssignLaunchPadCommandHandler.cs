using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Domain.Model;
using RocketLaunch.Domain.Service;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Application.Command.Handler;

public class AssignLaunchPadCommandHandler(IEventSourcingRepository repository, IResourceAvailabilityService validator)
    : CommandHandler<AssignLaunchPadCommand>(repository)
{
    public override async Task HandleCommandAsync(AssignLaunchPadCommand command)
    {
        Mission mission;

        try
        {
            mission = await AggregateSourcing.Source<Mission, MissionId>(command);
        }
        catch (Exception e)
        {
            throw new ApplicationProcessingException(HandlerErrors.ApplicationProcessingError, e);
        }

        await mission.AssignLaunchPadAsync(new LaunchPadId(command.LaunchPadId), validator);

        await AggregateRepository.SaveAsync(mission);
    }
}
