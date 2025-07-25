﻿using System.Diagnostics;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command.Mission;
using RocketLaunch.Application.Command.Mission.Handler;
using RocketLaunch.Application.Dto;
using RocketLaunch.Application.Tests.Mocks;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

// your command types / handlers
// your domain-event types

namespace RocketLaunch.Application.Tests.Mission;

public class AssignRocketCommandTests
{
    [Fact]
    public async Task Handle_AssignRocketCommand()
    {
        var validator = new StubResourceAvailabilityService();
        
        // 1. arrange: in-memory event store + repository
        var eventStore = new PureInMemoryEventStorageProvider();
        var repository = new EventSourcingRepository(eventStore /*, no snapshot provider*/);
            
        // 2. arrange: your command handler (inject repo + any other deps)
        var registerMissionCommandHandler = new RegisterMissionCommandHandler(repository /*, …*/);

        // 3. act: send the command
        var registerMissionCommand = new RegisterMissionCommand(
            missionId: Guid.NewGuid(),
            missionName: "Apollo 11",
            targetOrbit: "Above Surface of the Moon",
            payloadDescription: "Rover, 250kg",
            launchWindow: new LaunchWindowDto(DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromDays(6))
        );
            
        await registerMissionCommandHandler.HandleCommandAsync(registerMissionCommand);

        // 4. assert: check that exactly one MissionRegisteredEvent was stored
        var mission = await repository
            .GetByIdAsync<Domain.Model.Mission, MissionId>(new MissionId(registerMissionCommand.MissionId));
            
        Assert.NotNull(mission);
        
        var assignRocketCommandHandler = new AssignRocketCommandHandler(repository, validator);
        var assignRocketCommend = new AssignRocketCommand(registerMissionCommand.MissionId, Guid.NewGuid(),
            "Saturn V", 34.5, 140000, 3);
            
        await assignRocketCommandHandler.HandleCommandAsync(assignRocketCommend);
        
        mission = await repository
            .GetByIdAsync<Domain.Model.Mission, MissionId>(new MissionId(registerMissionCommand.MissionId));
        
        // 5. assert: check that the rocket was assigned
        Debug.Assert(mission != null, nameof(mission) + " != null");
        Assert.NotNull(mission.AssignedRocket);
        Assert.Equal(assignRocketCommend.RocketId, mission.AssignedRocket.Value);
        
        Assert.Equal(1, mission.CurrentVersion); // should be 1 since this is the second event for this aggregate
    }
}