using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Domain.Model;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Application.Command.Handler;

public class AssignCrewMemberCommandHandler(IEventSourcingRepository repository)
    : CommandHandler<AssignCrewMemberCommand>(repository)
{
    public override async Task HandleCommandAsync(AssignCrewMemberCommand command)
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

        crewMember.Assign();

        await AggregateRepository.SaveAsync(crewMember);
    }
}
