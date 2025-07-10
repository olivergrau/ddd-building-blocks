using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Application.Command.Mission.Handler;

public class AbortMissionCommandHandler(IEventSourcingRepository repository)
    : CommandHandler<AbortMissionCommand>(repository)
{
    public override async Task HandleCommandAsync(AbortMissionCommand command)
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

        mission.Abort();

        await AggregateRepository.SaveAsync(mission);
    }
}
