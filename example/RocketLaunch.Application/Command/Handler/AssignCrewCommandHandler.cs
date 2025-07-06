using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Domain.Model;
using RocketLaunch.Domain.Service;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Application.Command.Handler;

public class AssignCrewCommandHandler(IEventSourcingRepository repository, IResourceAvailabilityService validator)
    : CommandHandler<AssignCrewCommand>(repository)
{
    public override async Task HandleCommandAsync(AssignCrewCommand command)
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

        var crew = command.CrewMemberIds.Select(id => new CrewMemberId(id));
        await mission.AssignCrewAsync(crew, validator);

        await AggregateRepository.SaveAsync(mission);
    }
}
