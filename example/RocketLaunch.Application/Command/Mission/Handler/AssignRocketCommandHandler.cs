using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Domain.Model.Entities;
using RocketLaunch.Domain.Service;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Application.Command.Mission.Handler;

public class AssignRocketCommandHandler(IEventSourcingRepository repository, IResourceAvailabilityService validator) 
    : CommandHandler<AssignRocketCommand>(repository)
{
    public override async Task HandleCommandAsync(AssignRocketCommand command)
    {
        Domain.Model.Mission mission;

        try
        {
            mission =
                await AggregateSourcing.Source<Domain.Model.Mission, MissionId>(command);
        }
        catch (Exception e)
        {
            throw new ApplicationProcessingException(HandlerErrors.ApplicationProcessingError, e);
        }
        
        var rocket = new Rocket(new RocketId(command.RocketId), command.Name, command.ThrustCapacity, 
            command.PayloadCapacityKg, command.CrewCapacity);
        
        await mission.AssignRocketAsync(rocket, validator);
        
        await AggregateRepository.SaveAsync(mission);
    }
}