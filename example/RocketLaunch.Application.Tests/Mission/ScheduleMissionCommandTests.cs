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

public class ScheduleMissionCommandTests
{
    [Fact]
    public async Task Handle_ScheduleMissionCommand()
    {
        var validator = new StubResourceAvailabilityService();
        var eventStore = new PureInMemoryEventStorageProvider();
        var repository = new EventSourcingRepository(eventStore);

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
        await rocketHandler.HandleCommandAsync(new AssignRocketCommand(registerCommand.MissionId, Guid.NewGuid(),
            "Saturn V", 34.5, 140000, 3));

        var padHandler = new AssignLaunchPadCommandHandler(repository, validator);
        await padHandler.HandleCommandAsync(new AssignLaunchPadCommand(
            registerCommand.MissionId, Guid.NewGuid(), "LaunchPad-1", "Cape Canaveral", ["Ariane, Falcon 9"]));

        var handler = new ScheduleMissionCommandHandler(repository);
        await handler.HandleCommandAsync(new ScheduleMissionCommand(registerCommand.MissionId));

        var mission = await repository.GetByIdAsync<Domain.Model.Mission, MissionId>(new MissionId(registerCommand.MissionId));

        Debug.Assert(mission != null);
        Assert.Equal(MissionStatus.Scheduled, mission.Status);
        Assert.Equal(3, mission.CurrentVersion);
    }
}
