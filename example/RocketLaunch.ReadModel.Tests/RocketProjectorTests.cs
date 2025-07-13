using Microsoft.Extensions.Logging.Abstractions;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Projector;
using RocketLaunch.ReadModel.Core.Projector.Mission;
using RocketLaunch.ReadModel.InMemory.Service;
using RocketLaunch.ReadModel.Core.Service;
using RocketLaunch.ReadModel.Core.Exceptions;
using RocketLaunch.SharedKernel.Events.Mission;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.ReadModel.Tests;

public class RocketProjectorTests
{
    [Fact]
    public async Task RocketAssigned_marks_rocket_assigned()
    {
        var service = new InMemoryRocketService();
        var projector = new RocketProjector(service, NullLogger<RocketProjector>.Instance);

        var rocketId = Guid.NewGuid();
        await service.CreateOrUpdateAsync(new Rocket
        {
            RocketId = rocketId,
            Name = "Saturn",
            Status = RocketStatus.Available
        });

        var missionId = Guid.NewGuid();
        await projector.WhenAsync(
            new RocketAssigned(
                new MissionId(missionId), new RocketId(rocketId), "Falcon 9", 34.5, 140000, 3));

        var rocket = (await service.GetByIdAsync(rocketId))!;
        Assert.Equal(RocketStatus.Assigned, rocket.Status);
        Assert.Equal(missionId, rocket.AssignedMissionId);
    }

    [Fact]
    public async Task MissionAborted_releases_rocket()
    {
        var service = new InMemoryRocketService();
        var projector = new RocketProjector(service, NullLogger<RocketProjector>.Instance);

        var rocketId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        await service.CreateOrUpdateAsync(new Rocket
        {
            RocketId = rocketId,
            Name = "Saturn",
            Status = RocketStatus.Assigned,
            AssignedMissionId = missionId
        });

        await projector.WhenAsync(new MissionAborted(new MissionId(missionId)));

        var rocket = (await service.GetByIdAsync(rocketId))!;
        Assert.Equal(RocketStatus.Available, rocket.Status);
        Assert.Null(rocket.AssignedMissionId);
    }

    [Fact]
    public async Task CreateOrUpdate_failure_throws_service_exception()
    {
        var projector = new RocketProjector(new FailingRocketService(), NullLogger<RocketProjector>.Instance);
        await Assert.ThrowsAsync<ReadModelServiceException>(() =>
            projector.WhenAsync(new RocketAssigned(new MissionId(Guid.NewGuid()), new RocketId(Guid.NewGuid()), "R", 1, 1, 1)));
    }

    private class FailingRocketService : IRocketService
    {
        public Task<Rocket?> GetByIdAsync(Guid rocketId) => Task.FromResult<Rocket?>(null);
        public Task<IEnumerable<Rocket>> GetAllAsync() => Task.FromResult<IEnumerable<Rocket>>(Array.Empty<Rocket>());
        public Task<bool> IsAvailableAsync(Guid rocketId) => Task.FromResult(false);
        public Task<IEnumerable<Rocket>> FindAvailableAsync(int minPayloadKg, int minCrewCapacity) => Task.FromResult<IEnumerable<Rocket>>(Array.Empty<Rocket>());
        public Task<Rocket?> FindByAssignedMissionAsync(Guid missionId) => Task.FromResult<Rocket?>(null);
        public Task CreateOrUpdateAsync(Rocket rocket) => throw new Exception("fail");
    }
}
