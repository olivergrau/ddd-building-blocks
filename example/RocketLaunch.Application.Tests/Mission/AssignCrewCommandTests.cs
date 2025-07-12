using System.Diagnostics;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command.CrewMember;
using RocketLaunch.Application.Command.CrewMember.Handler;
using RocketLaunch.Application.Command.Mission;
using RocketLaunch.Application.Command.Mission.Handler;
using RocketLaunch.Domain.Service;
using RocketLaunch.Application.Dto;
using RocketLaunch.Application.Tests.Mocks;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Application.Tests.Mission;

public class AssignCrewCommandTests
{
    [Fact]
    public async Task Handle_AssignCrewCommand()
    {
        var validator = new StubResourceAvailabilityService();

        var eventStore = new PureInMemoryEventStorageProvider();
        var repository = new EventSourcingRepository(eventStore);

        var registerMissionHandler = new RegisterMissionCommandHandler(repository);
        var registerMissionCommand = new RegisterMissionCommand(
            missionId: Guid.NewGuid(),
            missionName: "Apollo 11",
            targetOrbit: "Above Surface of the Moon",
            payloadDescription: "Rover, 250kg",
            launchWindow: new LaunchWindowDto(DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromDays(6))
        );
        await registerMissionHandler.HandleCommandAsync(registerMissionCommand);

        var assignRocketHandler = new AssignRocketCommandHandler(repository, validator);
        await assignRocketHandler.HandleCommandAsync(new AssignRocketCommand(
            missionId: registerMissionCommand.MissionId,
            rocketId: Guid.NewGuid()
        ));

        var assignPadHandler = new AssignLaunchPadCommandHandler(repository, validator);
        await assignPadHandler.HandleCommandAsync(new AssignLaunchPadCommand(
            missionId: registerMissionCommand.MissionId,
            launchPadId: Guid.NewGuid()
        ));

        var crewAssignment = new CrewAssignment(validator);
        var assignCrewHandler = new AssignCrewCommandHandler(repository, crewAssignment);
        var crewIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var registerCrewHandler = new RegisterCrewMemberCommandHandler(repository);
        foreach (var id in crewIds)
        {
            await registerCrewHandler.HandleCommandAsync(new RegisterCrewMemberCommand(
                crewMemberId: id,
                name: $"Member-{id}",
                role: CrewRole.Commander,
                certifications: []));
        }
        await assignCrewHandler.HandleCommandAsync(new AssignCrewCommand(
            missionId: registerMissionCommand.MissionId,
            crewMemberIds: crewIds
        ));

        var mission = await repository.GetByIdAsync<Domain.Model.Mission, MissionId>(new MissionId(registerMissionCommand.MissionId));

        Debug.Assert(mission != null, nameof(mission) + " != null");
        Assert.Equal(crewIds.Length, mission.Crew.Count);
        foreach (var id in crewIds)
        {
            Assert.Contains(mission.Crew, m => Guid.Parse(m.AggregateId) == id);
        }
        Assert.Equal(3, mission.CurrentVersion);

        foreach (var id in crewIds)
        {
            var crew = await repository.GetByIdAsync<Domain.Model.CrewMember, CrewMemberId>(new CrewMemberId(id));
            Debug.Assert(crew != null);
            Assert.Equal(CrewMemberStatus.Assigned, crew.Status);
        }
    }
}
