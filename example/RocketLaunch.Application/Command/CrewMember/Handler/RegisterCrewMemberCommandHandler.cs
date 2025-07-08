using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Domain.Model;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Application.Command.Handler;

public class RegisterCrewMemberCommandHandler(IEventSourcingRepository repository)
    : CommandHandler<RegisterCrewMemberCommand>(repository)
{
    public override async Task HandleCommandAsync(RegisterCrewMemberCommand command)
    {
        CrewMember crewMember;

        try
        {
            crewMember = await AggregateSourcing.Source<CrewMember, CrewMemberId>(
                command,
                new CrewMemberId(command.CrewMemberId),
                command.Name,
                command.Role,
                command.Certifications
            );
        }
        catch (Exception e)
        {
            throw new ApplicationProcessingException(HandlerErrors.ApplicationProcessingError, e);
        }

        await AggregateRepository.SaveAsync(crewMember);
    }
}
