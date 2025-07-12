using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Application.Command.CrewMember.Handler;

public class AssignCrewMemberCommandHandler(IEventSourcingRepository repository)
    : CommandHandler<AssignCrewMemberCommand>(repository)
{
    public override async Task HandleCommandAsync(AssignCrewMemberCommand command)
    {
        Domain.Model.CrewMember crewMember;

        try
        {
            crewMember = await AggregateSourcing.Source<Domain.Model.CrewMember, CrewMemberId>(command);
        }
        catch (Exception e)
        {
            throw new ApplicationProcessingException(HandlerErrors.ApplicationProcessingError, e);
        }

        crewMember.Assign();

        await AggregateRepository.SaveAsync(crewMember);
    }
}
