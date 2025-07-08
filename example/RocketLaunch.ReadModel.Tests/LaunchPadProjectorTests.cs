using Microsoft.Extensions.Logging.Abstractions;
using RocketLaunch.ReadModel.Core.Builder;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.InMemory.Service;
using RocketLaunch.SharedKernel.Events.Mission;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.ReadModel.Tests;

public class LaunchPadProjectorTests
{
    [Fact]
    public async Task LaunchPadAssigned_marks_pad_occupied()
    {
        var service = new InMemoryLaunchPadService();
        var projector = new LaunchPadProjector(service, NullLogger<LaunchPadProjector>.Instance);

        var padId = Guid.NewGuid();
        await service.CreateOrUpdateAsync(new LaunchPad
        {
            LaunchPadId = padId,
            PadName = "Pad A",
            Status = LaunchPadStatus.Available
        });

        var missionId = Guid.NewGuid();
        var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));

        await projector.WhenAsync(new LaunchPadAssigned(new MissionId(missionId), new LaunchPadId(padId), window));

        var pad = service.GetById(padId)!;
        Assert.Equal(LaunchPadStatus.Occupied, pad.Status);
        Assert.Single(pad.OccupiedWindows);
        var scheduled = pad.OccupiedWindows[0];
        Assert.Equal(missionId, scheduled.MissionId);
        Assert.Equal(window.Start, scheduled.Start);
        Assert.Equal(window.End, scheduled.End);
    }

    [Fact]
    public async Task MissionAborted_releases_launch_pad()
    {
        var service = new InMemoryLaunchPadService();
        var projector = new LaunchPadProjector(service, NullLogger<LaunchPadProjector>.Instance);

        var padId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        await service.CreateOrUpdateAsync(new LaunchPad
        {
            LaunchPadId = padId,
            PadName = "Pad B",
            Status = LaunchPadStatus.Occupied,
            OccupiedWindows =
            [
                new ScheduledLaunchWindow
                {
                    MissionId = missionId,
                    Start = window.Start,
                    End = window.End
                }
            ]
        });

        await projector.WhenAsync(new MissionAborted(new MissionId(missionId)));

        var pad = service.GetById(padId)!;
        Assert.Equal(LaunchPadStatus.Available, pad.Status);
        Assert.Empty(pad.OccupiedWindows);
    }
    
    [Fact]
    public async Task MissionAborted_releases_launch_pad_after_rocket_launch()
    {
        var service = new InMemoryLaunchPadService();
        var projector = new LaunchPadProjector(service, NullLogger<LaunchPadProjector>.Instance);

        var padId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        await service.CreateOrUpdateAsync(new LaunchPad
        {
            LaunchPadId = padId,
            PadName = "Pad B",
            Status = LaunchPadStatus.Occupied,
            OccupiedWindows =
            [
                new ScheduledLaunchWindow
                {
                    MissionId = missionId,
                    Start = window.Start,
                    End = window.End
                }
            ]
        });

        await projector.WhenAsync(new MissionLaunched(new MissionId(missionId)));

        var pad = service.GetById(padId)
            ?? throw new InvalidOperationException("Launch pad not found");
        Assert.Equal(LaunchPadStatus.Available, pad.Status);
        Assert.Empty(pad.OccupiedWindows);
    }
}
