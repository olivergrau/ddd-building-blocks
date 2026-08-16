using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Application.Command.CrewMember;
using RocketLaunch.Domain.Service;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Application.Command.Mission.Handler;

public class AssignCrewCommandHandler(IEventSourcingRepository repository, CrewAssignment crewAssignment)
    : CommandHandler<AssignCrewCommand>(repository)
{
    private readonly CrewAssignment _crewAssignment = crewAssignment;
    public override async Task HandleAsync(AssignCrewCommand command, System.Threading.CancellationToken cancellationToken)
    {
        Domain.Model.Mission mission;
        try
        {
            mission = await AggregateSourcing.Source<Domain.Model.Mission, MissionId>(command, [], cancellationToken);
        }
        catch (Exception e)
        {
            throw new ApplicationProcessingException(HandlerErrors.ApplicationProcessingError, e);
        }

        var crewMemberAggregates = new List<Domain.Model.CrewMember>();
        foreach (var id in command.CrewMemberIds)
        {
            var crewCmd = new AssignCrewMemberCommand(id);
            var member = await AggregateSourcing.Source<Domain.Model.CrewMember, CrewMemberId>(crewCmd, [], cancellationToken);
            crewMemberAggregates.Add(member);
        }

        await _crewAssignment.AssignAsync(mission, crewMemberAggregates);

        await AggregateRepository.SaveAsync(mission, cancellationToken);
        foreach (var crewMember in crewMemberAggregates)
        {
            await AggregateRepository.SaveAsync(crewMember, cancellationToken);
        }
    }
}
