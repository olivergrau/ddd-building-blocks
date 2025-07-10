using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using RocketLaunch.Domain.Model;
using RocketLaunch.Domain.Service;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Application.Command.Mission.Handler;

public class AssignCrewCommandHandler(IEventSourcingRepository repository, CrewAssignment crewAssignment)
    : CommandHandler<AssignCrewCommand>(repository)
{
    private readonly CrewAssignment _crewAssignment = crewAssignment;
    public override async Task HandleCommandAsync(AssignCrewCommand command)
    {
        Domain.Model.Mission mission;
        try
        {
            mission = await AggregateSourcing.Source<Domain.Model.Mission, MissionId>(command);
        }
        catch (Exception e)
        {
            throw new ApplicationProcessingException(HandlerErrors.ApplicationProcessingError, e);
        }

        var crewMemberAggregates = new List<CrewMember>();
        foreach (var id in command.CrewMemberIds)
        {
            var crewCmd = new AssignCrewMemberCommand(id);
            var member = await AggregateSourcing.Source<CrewMember, CrewMemberId>(crewCmd);
            crewMemberAggregates.Add(member);
        }

        await _crewAssignment.AssignAsync(mission, crewMemberAggregates);

        await AggregateRepository.SaveAsync(mission);
        foreach (var crewMember in crewMemberAggregates)
        {
            await AggregateRepository.SaveAsync(crewMember);
        }
    }
}
