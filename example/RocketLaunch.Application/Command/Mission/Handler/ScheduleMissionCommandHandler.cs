using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Domain.Model;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Application.Command.Handler;

public class ScheduleMissionCommandHandler(IEventSourcingRepository repository)
    : CommandHandler<ScheduleMissionCommand>(repository)
{
    public override async Task HandleCommandAsync(ScheduleMissionCommand command)
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

        mission.Schedule();

        await AggregateRepository.SaveAsync(mission);
    }
}
