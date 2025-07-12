using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command;
using RocketLaunch.Application.Command.Mission;
using RocketLaunch.Application.Command.Mission.Handler;
using RocketLaunch.Application.Dto;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

// your command types / handlers
// your domain-event types

namespace RocketLaunch.Application.Tests.Mission;

public class RegisterMissionCommandTests
{
    [Fact]
    public async Task Handle_RegisterMissionCommand_PersistsMissionRegisteredEvent()
    {
        // 1. arrange: in-memory event store + repository
        var eventStore = new PureInMemoryEventStorageProvider();
        var repository = new EventSourcingRepository(eventStore /*, no snapshot provider*/);
            
        // 2. arrange: your command handler (inject repo + any other deps)
        var handler = new RegisterMissionCommandHandler(repository /*, …*/);

        // 3. act: send the command
        var command = new RegisterMissionCommand(
            missionId: Guid.NewGuid(),
            missionName: "Apollo 11",
            targetOrbit: "Above Surface of the Moon",
            payloadDescription: "Rover, 250kg",
            launchWindow: new LaunchWindowDto(DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromDays(6))
        );
            
        await handler.HandleCommandAsync(command);

        // 4. assert: check that exactly one MissionRegisteredEvent was stored
        var mission = await repository
            .GetByIdAsync<Domain.Model.Mission, MissionId>(new MissionId(command.MissionId));
            
        Assert.NotNull(mission);
        
        Assert.Equal(0, mission.CurrentVersion); // should be 0 since this is the first event for this aggregate
        
        // --- verify identity ---
        Assert.Equal(command.MissionId, mission.Id.Value);

        // --- verify all properties were applied ---
        Assert.Equal(new MissionName(command.MissionName), mission.Name);
        Assert.Equal(new TargetOrbit(command.TargetOrbit), mission.TargetOrbit);
        Assert.Equal(new PayloadDescription(command.PayloadDescription), mission.Payload);

        // assuming your domain LaunchWindow maps exactly from the DTO
        Assert.Equal(command.LaunchWindow.Start,    mission.Window.Start);
        Assert.Equal(command.LaunchWindow.End,      mission.Window.End);
    }
}