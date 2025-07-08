using Microsoft.Extensions.Logging.Abstractions;
using RocketLaunch.ReadModel.Core.Builder;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.InMemory.Service;
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
            RocketName = "Saturn",
            Status = RocketStatus.Available
        });

        var missionId = Guid.NewGuid();
        await projector.WhenAsync(new RocketAssigned(new MissionId(missionId), new RocketId(rocketId)));

        var rocket = service.GetById(rocketId)!;
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
            RocketName = "Saturn",
            Status = RocketStatus.Assigned,
            AssignedMissionId = missionId
        });

        await projector.WhenAsync(new MissionAborted(new MissionId(missionId)));

        var rocket = service.GetById(rocketId)!;
        Assert.Equal(RocketStatus.Available, rocket.Status);
        Assert.Null(rocket.AssignedMissionId);
    }
}
