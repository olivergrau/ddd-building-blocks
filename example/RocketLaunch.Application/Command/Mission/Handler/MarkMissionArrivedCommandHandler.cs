using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Application.Dto;
using RocketLaunch.Domain.Model;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Application.Command.Handler;

public class MarkMissionArrivedCommandHandler(IEventSourcingRepository repository)
    : CommandHandler<MarkMissionArrivedCommand>(repository)
{
    public override async Task HandleCommandAsync(MarkMissionArrivedCommand command)
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

        var crew = command.CrewManifest.Select(c => (c.Name, c.Role));
        var payload = command.PayloadManifest.Select(p => (p.Item, p.Mass));
        mission.MarkArrived(command.ArrivalTime, command.VehicleType, crew, payload);

        await AggregateRepository.SaveAsync(mission);
    }
}
