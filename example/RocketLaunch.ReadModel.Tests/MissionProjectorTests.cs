using Microsoft.Extensions.Logging.Abstractions;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Projector.Mission;
using RocketLaunch.ReadModel.InMemory.Service;
using RocketLaunch.SharedKernel.Events.Mission;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;
using RocketLaunch.ReadModel.Core.Exceptions;
using Xunit;
using CrewMemberStatus = RocketLaunch.ReadModel.Core.Model.CrewMemberStatus;

namespace RocketLaunch.ReadModel.Tests;

public class MissionProjectorTests
{
    [Fact]
    public async Task MissionCreated_creates_mission()
    {
        var service = new InMemoryMissionService();
        var crewService = new InMemoryCrewService(service);
        var projector = new MissionProjector(service, crewService, NullLogger<MissionProjector>.Instance);

        var missionId = Guid.NewGuid();
        var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        await projector.WhenAsync(new MissionCreated(new MissionId(missionId), new MissionName("Test"), new TargetOrbit("LEO"), new PayloadDescription("Sat"), window));

        var mission = (await service.GetByIdAsync(missionId))!;
        Assert.Equal("Test", mission.Name);
        Assert.Equal(MissionStatus.Planned, mission.Status);
        Assert.Equal(window.Start, mission.LaunchWindowStart);
    }

    [Fact]
    public async Task CrewAssigned_adds_members()
    {
        var service = new InMemoryMissionService();
        var crewService = new InMemoryCrewService(service);
        var projector = new MissionProjector(service, crewService, NullLogger<MissionProjector>.Instance);
        var missionId = Guid.NewGuid();
        var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        await projector.WhenAsync(
            new MissionCreated(
                new MissionId(missionId), new MissionName("Test"), new TargetOrbit("LEO"), new PayloadDescription("Sat"), window));

        var crewIds = new[] { new CrewMemberId(Guid.NewGuid()), new CrewMemberId(Guid.NewGuid()) };
        
        await crewService.CreateOrUpdateAsync(new CrewMember
        {
            CrewMemberId = crewIds[0].Value,
            Name = "Alice",
            Role = "Pilot",
            CertificationLevels = ["Basic"],
            Status = CrewMemberStatus.Assigned
        });
        
        await crewService.CreateOrUpdateAsync(new CrewMember
        {
            CrewMemberId = crewIds[1].Value,
            Name = "Bob",
            Role = "Engineer",
            CertificationLevels = ["Advanced"],
            Status = CrewMemberStatus.Assigned
        });
        
        await projector.WhenAsync(new CrewAssigned(new MissionId(missionId), crewIds));

        var mission = (await service.GetByIdAsync(missionId))!;
        Assert.Equal(2, mission.CrewMemberIds.Count);
        Assert.Contains(crewIds[0].Value, mission.CrewMemberIds);
        Assert.Contains(crewIds[1].Value, mission.CrewMemberIds);
    }

    [Fact]
    public async Task RocketAssigned_unknown_mission_throws()
    {
        var service = new InMemoryMissionService();
        var crewService = new InMemoryCrewService(service);
        var projector = new MissionProjector(service, crewService, NullLogger<MissionProjector>.Instance);

        await Assert.ThrowsAsync<ReadModelException>(() =>
            projector.WhenAsync(
                new RocketAssigned(
                    new MissionId(Guid.NewGuid()),
                    new RocketId(Guid.NewGuid()),
                    "Rocket",
                    1.0,
                    1,
                    1)));
    }

    [Fact]
    public async Task CrewAssigned_unknown_member_throws()
    {
        var service = new InMemoryMissionService();
        var crewService = new InMemoryCrewService(service);
        var projector = new MissionProjector(service, crewService, NullLogger<MissionProjector>.Instance);

        var missionId = Guid.NewGuid();
        var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        await projector.WhenAsync(new MissionCreated(new MissionId(missionId), new MissionName("Test"), new TargetOrbit("LEO"), new PayloadDescription("Sat"), window));

        await Assert.ThrowsAsync<ReadModelException>(() =>
            projector.WhenAsync(
                new CrewAssigned(
                    new MissionId(missionId),
                    new[] { new CrewMemberId(Guid.NewGuid()) })));        
    }
}
