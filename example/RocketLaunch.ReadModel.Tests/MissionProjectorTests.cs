using Microsoft.Extensions.Logging.Abstractions;
using RocketLaunch.ReadModel.Core.Projector.Mission;
using RocketLaunch.ReadModel.InMemory.Service;
using RocketLaunch.SharedKernel.Events.Mission;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.ReadModel.Tests;

public class MissionProjectorTests
{
    [Fact]
    public async Task MissionCreated_creates_mission()
    {
        var service = new InMemoryMissionService();
        var projector = new MissionProjector(service, NullLogger<MissionProjector>.Instance);

        var missionId = Guid.NewGuid();
        var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        await projector.WhenAsync(new MissionCreated(new MissionId(missionId), new MissionName("Test"), new TargetOrbit("LEO"), new PayloadDescription("Sat"), window));

        var mission = service.GetById(missionId)!;
        Assert.Equal("Test", mission.Name);
        Assert.Equal(MissionStatus.Planned, mission.Status);
        Assert.Equal(window.Start, mission.LaunchWindowStart);
    }

    [Fact]
    public async Task CrewAssigned_adds_members()
    {
        var service = new InMemoryMissionService();
        var projector = new MissionProjector(service, NullLogger<MissionProjector>.Instance);
        var missionId = Guid.NewGuid();
        var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        await projector.WhenAsync(new MissionCreated(new MissionId(missionId), new MissionName("Test"), new TargetOrbit("LEO"), new PayloadDescription("Sat"), window));

        var crewIds = new[] { new CrewMemberId(Guid.NewGuid()), new CrewMemberId(Guid.NewGuid()) };
        await projector.WhenAsync(new CrewAssigned(new MissionId(missionId), crewIds));

        var mission = service.GetById(missionId)!;
        Assert.Equal(2, mission.CrewMemberIds.Count);
        Assert.Contains(crewIds[0].Value, mission.CrewMemberIds);
        Assert.Contains(crewIds[1].Value, mission.CrewMemberIds);
    }
}
