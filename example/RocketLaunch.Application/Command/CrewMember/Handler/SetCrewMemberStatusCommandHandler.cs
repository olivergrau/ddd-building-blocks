using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Domain.Model;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Application.Command.Handler;

public class SetCrewMemberStatusCommandHandler(IEventSourcingRepository repository)
    : CommandHandler<SetCrewMemberStatusCommand>(repository)
{
    public override async Task HandleCommandAsync(SetCrewMemberStatusCommand command)
    {
        CrewMember crewMember;

        try
        {
            crewMember = await AggregateSourcing.Source<CrewMember, CrewMemberId>(command);
        }
        catch (Exception e)
        {
            throw new ApplicationProcessingException(HandlerErrors.ApplicationProcessingError, e);
        }

        crewMember.SetStatus(command.Status);

        await AggregateRepository.SaveAsync(crewMember);
    }
}
