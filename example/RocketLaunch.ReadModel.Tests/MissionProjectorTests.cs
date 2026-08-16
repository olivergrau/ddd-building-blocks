using Microsoft.Extensions.Logging.Abstractions;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Projector.Mission;
using RocketLaunch.ReadModel.InMemory.Service;
using RocketLaunch.ReadModel.Core.Service;
using RocketLaunch.ReadModel.Core.Exceptions;
using RocketLaunch.SharedKernel.Events.Mission;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;
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
        await projector.HandleAsync(new MissionCreated(new MissionId(missionId), new MissionName("Test"), new TargetOrbit("LEO"), new PayloadDescription("Sat"), window), System.Threading.CancellationToken.None);

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
        await projector.HandleAsync(
            new MissionCreated(
                new MissionId(missionId), new MissionName("Test"), new TargetOrbit("LEO"), new PayloadDescription("Sat"), window), System.Threading.CancellationToken.None);

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
        
        await projector.HandleAsync(new CrewAssigned(new MissionId(missionId), crewIds), System.Threading.CancellationToken.None);

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
            projector.HandleAsync(
                new RocketAssigned(
                    new MissionId(Guid.NewGuid()),
                    new RocketId(Guid.NewGuid()),
                    "Rocket",
                    1.0,
                    1,
                    1), System.Threading.CancellationToken.None));
    }

    [Fact]
    public async Task CrewAssigned_unknown_member_throws()
    {
        var service = new InMemoryMissionService();
        var crewService = new InMemoryCrewService(service);
        var projector = new MissionProjector(service, crewService, NullLogger<MissionProjector>.Instance);

        var missionId = Guid.NewGuid();
        var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        await projector.HandleAsync(new MissionCreated(new MissionId(missionId), new MissionName("Test"), new TargetOrbit("LEO"), new PayloadDescription("Sat"), window), System.Threading.CancellationToken.None);

        await Assert.ThrowsAsync<ReadModelException>(() =>
            projector.HandleAsync(
                new CrewAssigned(
                    new MissionId(missionId),
                    new[] { new CrewMemberId(Guid.NewGuid()) }), System.Threading.CancellationToken.None));
    }

    [Fact]
    public async Task CreateOrUpdate_failure_throws_service_exception()
    {
        var crewService = new InMemoryCrewService(new InMemoryMissionService());
        var projector = new MissionProjector(new FailingMissionService(), crewService, NullLogger<MissionProjector>.Instance);

        var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));

        await Assert.ThrowsAsync<ReadModelServiceException>(() =>
            projector.HandleAsync(new MissionCreated(new MissionId(Guid.NewGuid()), new MissionName("T"), new TargetOrbit("L"), new PayloadDescription("P"), window), System.Threading.CancellationToken.None));
    }

    private class FailingMissionService : IMissionService
    {
        public Task<Mission?> GetByIdAsync(Guid missionId) => Task.FromResult<Mission?>(null);
        public Task<IEnumerable<Mission>> GetAllAsync() => Task.FromResult<IEnumerable<Mission>>(Array.Empty<Mission>());
        public Task CreateOrUpdateAsync(Mission mission) => throw new Exception("fail");
    }
}
