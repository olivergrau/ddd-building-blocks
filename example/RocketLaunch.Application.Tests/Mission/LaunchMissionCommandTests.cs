using System.Diagnostics;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command.Mission;
using RocketLaunch.Application.Command.Mission.Handler;
using RocketLaunch.Application.Dto;
using RocketLaunch.Application.Tests.Mocks;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Application.Tests.Mission;

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
        await registerHandler.HandleAsync(registerCommand, System.Threading.CancellationToken.None);

        var rocketHandler = new AssignRocketCommandHandler(repository, validator);
        await rocketHandler.HandleAsync(new AssignRocketCommand(registerCommand.MissionId, Guid.NewGuid(),
            "Saturn V", 34.5, 140000, 3), System.Threading.CancellationToken.None);

        var padHandler = new AssignLaunchPadCommandHandler(repository, validator);
        await padHandler.HandleAsync(new AssignLaunchPadCommand(
            registerCommand.MissionId, Guid.NewGuid(), "LaunchPad-1", "Cape Canaveral", ["Ariane, Falcon 9"]), System.Threading.CancellationToken.None);

        var scheduleHandler = new ScheduleMissionCommandHandler(repository);
        await scheduleHandler.HandleAsync(new ScheduleMissionCommand(registerCommand.MissionId), System.Threading.CancellationToken.None);

        var handler = new LaunchMissionCommandHandler(repository);
        await handler.HandleAsync(new LaunchMissionCommand(registerCommand.MissionId), System.Threading.CancellationToken.None);

        var mission = await repository.GetByIdAsync<Domain.Model.Mission, MissionId>(new MissionId(registerCommand.MissionId), System.Threading.CancellationToken.None);
        Debug.Assert(mission != null);
        Assert.Equal(MissionStatus.Launched, mission.Status);
        Assert.Equal(4, mission.CurrentVersion);
    }
}
