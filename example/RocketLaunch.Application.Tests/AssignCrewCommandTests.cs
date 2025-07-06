using System.Diagnostics;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command;
using RocketLaunch.Application.Command.Handler;
using RocketLaunch.Application.Dto;
using RocketLaunch.Application.Tests.Mocks;
using RocketLaunch.Domain.Model;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Application.Tests;

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

        var assignCrewHandler = new AssignCrewCommandHandler(repository, validator);
        var crewIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        await assignCrewHandler.HandleCommandAsync(new AssignCrewCommand(
            missionId: registerMissionCommand.MissionId,
            crewMemberIds: crewIds
        ));

        var mission = await repository.GetByIdAsync<Mission, MissionId>(new MissionId(registerMissionCommand.MissionId));

        Debug.Assert(mission != null, nameof(mission) + " != null");
        Assert.Equal(crewIds.Length, mission!.Crew.Count);
        foreach (var id in crewIds)
        {
            Assert.Contains(mission.Crew, m => m.Value == id);
        }
        Assert.Equal(3, mission.CurrentVersion);
    }
}
