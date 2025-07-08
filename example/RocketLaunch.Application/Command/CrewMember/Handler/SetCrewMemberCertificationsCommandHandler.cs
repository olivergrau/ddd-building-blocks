using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Domain.Model;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Application.Command.Handler;

public class SetCrewMemberCertificationsCommandHandler(IEventSourcingRepository repository)
    : CommandHandler<SetCrewMemberCertificationsCommand>(repository)
{
    public override async Task HandleCommandAsync(SetCrewMemberCertificationsCommand command)
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

        crewMember.SetCertifications(command.Certifications);

        await AggregateRepository.SaveAsync(crewMember);
    }
}
