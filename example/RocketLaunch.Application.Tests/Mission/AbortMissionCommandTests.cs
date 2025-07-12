using System.Diagnostics;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command.Mission;
using RocketLaunch.Application.Command.Mission.Handler;
using RocketLaunch.Application.Dto;
using RocketLaunch.Domain.Service;
using RocketLaunch.Application.Command.CrewMember;
using RocketLaunch.Application.Command.CrewMember.Handler;
using RocketLaunch.Application.Tests.Mocks;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Application.Tests.Mission;

public class AbortMissionCommandTests
{
    [Fact]
    public async Task Handle_AbortMissionCommand()
    {
        var validator = new StubResourceAvailabilityService();
        var store = new PureInMemoryEventStorageProvider();
        var repository = new EventSourcingRepository(store);

        var registerHandler = new RegisterMissionCommandHandler(repository);
        var registerCommand = new RegisterMissionCommand(
            missionId: Guid.NewGuid(),
            missionName: "Apollo 11",
            targetOrbit: "Moon",
            payloadDescription: "Rover",
            launchWindow: new LaunchWindowDto(DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromDays(6))
        );
        await registerHandler.HandleCommandAsync(registerCommand);

        var rocketHandler = new AssignRocketCommandHandler(repository, validator);
        await rocketHandler.HandleCommandAsync(new AssignRocketCommand(registerCommand.MissionId, Guid.NewGuid()));

        var padHandler = new AssignLaunchPadCommandHandler(repository, validator);
        await padHandler.HandleCommandAsync(new AssignLaunchPadCommand(registerCommand.MissionId, Guid.NewGuid()));

        var crewAssignment = new CrewAssignment(validator);
        var assignCrewHandler = new AssignCrewCommandHandler(repository, crewAssignment);
        var crewIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var registerCrewHandler = new RegisterCrewMemberCommandHandler(repository);
        foreach (var id in crewIds)
        {
            await registerCrewHandler.HandleCommandAsync(new RegisterCrewMemberCommand(id, $"Member-{id}", CrewRole.Commander, []));
        }
        await assignCrewHandler.HandleCommandAsync(new AssignCrewCommand(registerCommand.MissionId, crewIds));

        var scheduleHandler = new ScheduleMissionCommandHandler(repository);
        await scheduleHandler.HandleCommandAsync(new ScheduleMissionCommand(registerCommand.MissionId));

        var unassignment = new CrewUnassignment();
        var handler = new AbortMissionCommandHandler(repository, unassignment);
        await handler.HandleCommandAsync(new AbortMissionCommand(registerCommand.MissionId));

        var mission = await repository.GetByIdAsync<Domain.Model.Mission, MissionId>(new MissionId(registerCommand.MissionId));
        Debug.Assert(mission != null);
        Assert.Equal(MissionStatus.Aborted, mission.Status);
        Assert.Equal(5, mission.CurrentVersion);

        foreach (var id in crewIds)
        {
            var crewMember = await repository.GetByIdAsync<Domain.Model.CrewMember, CrewMemberId>(new CrewMemberId(id));
            Debug.Assert(crewMember != null);
            Assert.Equal(CrewMemberStatus.Available, crewMember.Status);
        }
    }
}
