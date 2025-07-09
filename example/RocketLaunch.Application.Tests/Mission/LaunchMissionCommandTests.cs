using System.Diagnostics;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command;
using RocketLaunch.Application.Command.Handler;
using RocketLaunch.Application.Dto;
using RocketLaunch.Application.Tests.Mocks;
using RocketLaunch.Domain.Model;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Application.Tests;

public class LaunchMissionCommandTests
{
    [Fact]
    public async Task Handle_LaunchMissionCommand()
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

        var scheduleHandler = new ScheduleMissionCommandHandler(repository);
        await scheduleHandler.HandleCommandAsync(new ScheduleMissionCommand(registerCommand.MissionId));

        var handler = new LaunchMissionCommandHandler(repository);
        await handler.HandleCommandAsync(new LaunchMissionCommand(registerCommand.MissionId));

        var mission = await repository.GetByIdAsync<Mission, MissionId>(new MissionId(registerCommand.MissionId));
        Debug.Assert(mission != null);
        Assert.Equal(MissionStatus.Launched, mission.Status);
        Assert.Equal(4, mission.CurrentVersion);
    }
}
