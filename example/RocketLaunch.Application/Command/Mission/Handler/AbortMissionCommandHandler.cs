using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using System;
using RocketLaunch.SharedKernel.ValueObjects;
using RocketLaunch.Application.Command.CrewMember;
using RocketLaunch.Domain.Service;

namespace RocketLaunch.Application.Command.Mission.Handler;

public class AbortMissionCommandHandler(IEventSourcingRepository repository, CrewUnassignment crewUnassignment)
    : CommandHandler<AbortMissionCommand>(repository)
{
    private readonly CrewUnassignment _crewUnassignment = crewUnassignment;
    public override async Task HandleCommandAsync(AbortMissionCommand command)
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

        var crewMemberAggregates = new List<Domain.Model.CrewMember>();
        foreach (var relation in mission.Crew)
        {
            var crewCmd = new ReleaseCrewMemberCommand(Guid.Parse(relation.AggregateId));
            var member = await AggregateSourcing.Source<Domain.Model.CrewMember, CrewMemberId>(crewCmd);
            crewMemberAggregates.Add(member);
        }

        _crewUnassignment.Unassign(mission, crewMemberAggregates);

        await AggregateRepository.SaveAsync(mission);
        foreach (var crewMember in crewMemberAggregates)
        {
            await AggregateRepository.SaveAsync(crewMember);
        }
    }
}
