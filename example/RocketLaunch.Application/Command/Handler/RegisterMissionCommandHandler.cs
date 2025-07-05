using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Domain.Model;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Application.Command.Handler;

public class RegisterMissionCommandHandler(IEventSourcingRepository repository) 
    : CommandHandler<RegisterMissionCommand>(repository)
{
    public override async Task HandleCommandAsync(RegisterMissionCommand command)
    {
        Mission mission;

        try
        {
            mission =
                await AggregateSourcing.Source<Mission, MissionId>(
                    command, 
                    new MissionId(command.MissionId), 
                    new TargetOrbit(command.TargetOrbit), 
                    new PayloadDescription(command.PayloadDescription),
                    new LaunchWindow(
                        command.LaunchWindow.Start, 
                        command.LaunchWindow.End
                    )
                );
        }
        catch (Exception e)
        {
            throw new ApplicationProcessingException(HandlerErrors.ApplicationProcessingError, e);
        }

        await AggregateRepository.SaveAsync(mission);
    }
}