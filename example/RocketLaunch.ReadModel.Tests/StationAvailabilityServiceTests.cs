using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.InMemory.Service;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.ReadModel.Tests;

public class StationAvailabilityServiceTests
{
    [Fact]
    public async Task IsRocketAvailable_returns_true_when_available()
    {
        var rocketService = new InMemoryRocketService();
        var padService = new InMemoryLaunchPadService();
        var crewService = new InMemoryCrewService();
        var sut = new InMemoryStationAvailabilityService(rocketService, padService, crewService);

        var rocketId = Guid.NewGuid();
        await rocketService.CreateOrUpdateAsync(new Rocket
        {
            RocketId = rocketId,
            RocketName = "Falcon",
            Status = RocketStatus.Available
        });

        var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        var available = await sut.IsRocketAvailableAsync(new RocketId(rocketId), window);

        Assert.True(available);
    }

    [Fact]
    public async Task IsRocketAvailable_returns_false_when_not_available()
    {
        var rocketService = new InMemoryRocketService();
        var sut = new InMemoryStationAvailabilityService(rocketService, new InMemoryLaunchPadService(), new InMemoryCrewService());

        var rocketId = Guid.NewGuid();
        await rocketService.CreateOrUpdateAsync(new Rocket
        {
            RocketId = rocketId,
            RocketName = "Falcon",
            Status = RocketStatus.Assigned,
            AssignedMissionId = Guid.NewGuid()
        });

        var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        var available = await sut.IsRocketAvailableAsync(new RocketId(rocketId), window);

        Assert.False(available);
    }

    [Fact]
    public async Task IsLaunchPadAvailable_returns_true_when_no_overlap_and_not_under_maintenance()
    {
        var rocketService = new InMemoryRocketService();
        var padService = new InMemoryLaunchPadService();
        var crewService = new InMemoryCrewService();
        var sut = new InMemoryStationAvailabilityService(rocketService, padService, crewService);

        var padId = Guid.NewGuid();
        await padService.CreateOrUpdateAsync(new LaunchPad
        {
            LaunchPadId = padId,
            PadName = "Pad 1",
            Status = LaunchPadStatus.Available,
            SupportedRocketTypes = ["Falcon"]
        });

        var window = new LaunchWindow(DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2));
        var available = await sut.IsLaunchPadAvailableAsync(new LaunchPadId(padId), window);

        Assert.True(available);
    }

    [Fact]
    public async Task IsLaunchPadAvailable_returns_false_when_overlap()
    {
        var padService = new InMemoryLaunchPadService();
        var sut = new InMemoryStationAvailabilityService(new InMemoryRocketService(), padService, new InMemoryCrewService());

        var padId = Guid.NewGuid();
        var window1 = new LaunchWindow(DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2));
        await padService.CreateOrUpdateAsync(new LaunchPad
        {
            LaunchPadId = padId,
            PadName = "Pad 2",
            Status = LaunchPadStatus.Available,
            OccupiedWindows = [
                new ScheduledLaunchWindow
                {
                    MissionId = Guid.NewGuid(),
                    Start = window1.Start,
                    End = window1.End
                }
            ]
        });

        var checkWindow = new LaunchWindow(DateTime.UtcNow.AddHours(1.5), DateTime.UtcNow.AddHours(2.5));
        var available = await sut.IsLaunchPadAvailableAsync(new LaunchPadId(padId), checkWindow);

        Assert.False(available);
    }

    [Fact]
    public async Task AreCrewMembersAvailable_returns_true_when_all_available()
    {
        var crewService = new InMemoryCrewService();
        var sut = new InMemoryStationAvailabilityService(new InMemoryRocketService(), new InMemoryLaunchPadService(), crewService);

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        await crewService.CreateOrUpdateAsync(new CrewMember { CrewMemberId = id1, Name = "A", Role = "Pilot", Status = CrewMemberStatus.Available });
        await crewService.CreateOrUpdateAsync(new CrewMember { CrewMemberId = id2, Name = "B", Role = "Pilot", Status = CrewMemberStatus.Available });

        var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        var available = await sut.AreCrewMembersAvailableAsync(new[] { new CrewMemberId(id1), new CrewMemberId(id2) }, window);

        Assert.True(available);
    }

    [Fact]
    public async Task AreCrewMembersAvailable_returns_false_when_any_unavailable()
    {
        var crewService = new InMemoryCrewService();
        var sut = new InMemoryStationAvailabilityService(new InMemoryRocketService(), new InMemoryLaunchPadService(), crewService);

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        await crewService.CreateOrUpdateAsync(new CrewMember { CrewMemberId = id1, Name = "A", Role = "Pilot", Status = CrewMemberStatus.Available });
        await crewService.CreateOrUpdateAsync(new CrewMember { CrewMemberId = id2, Name = "B", Role = "Pilot", Status = CrewMemberStatus.Assigned });

        var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        var available = await sut.AreCrewMembersAvailableAsync(new[] { new CrewMemberId(id1), new CrewMemberId(id2) }, window);

        Assert.False(available);
    }
}
