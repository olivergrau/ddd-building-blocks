using System.Diagnostics;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command;
using RocketLaunch.Application.Command.Mission;
using RocketLaunch.Application.Command.Mission.Handler;
using RocketLaunch.Application.Dto;
using RocketLaunch.Application.Tests.Mocks;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Application.Tests.Mission;

public class AssignLaunchPadCommandTests
{
    [Fact]
    public async Task Handle_AssignLaunchPadCommand()
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
        var assignRocketCommand = new AssignRocketCommand(
            missionId: registerMissionCommand.MissionId,
            rocketId: Guid.NewGuid()
        );
        await assignRocketHandler.HandleCommandAsync(assignRocketCommand);

        var assignPadHandler = new AssignLaunchPadCommandHandler(repository, validator);
        var padId = Guid.NewGuid();
        var assignPadCommand = new AssignLaunchPadCommand(
            missionId: registerMissionCommand.MissionId,
            launchPadId: padId
        );
        await assignPadHandler.HandleCommandAsync(assignPadCommand);

        var mission = await repository.GetByIdAsync<Domain.Model.Mission, MissionId>(new MissionId(registerMissionCommand.MissionId));

        Debug.Assert(mission != null, nameof(mission) + " != null");
        Assert.NotNull(mission.AssignedPad);
        Assert.Equal(padId, mission.AssignedPad.Value);
        Assert.Equal(2, mission.CurrentVersion);
    }
}
