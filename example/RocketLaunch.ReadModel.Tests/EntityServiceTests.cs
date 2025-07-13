using System.Linq;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.InMemory.Service;
using Xunit;

namespace RocketLaunch.ReadModel.Tests;

public class EntityServiceTests
{
    [Fact]
    public async Task RocketService_returns_all_rockets()
    {
        var service = new InMemoryRocketService();
        await service.CreateOrUpdateAsync(new Rocket { RocketId = Guid.NewGuid(), Name = "A" });
        await service.CreateOrUpdateAsync(new Rocket { RocketId = Guid.NewGuid(), Name = "B" });

        var all = (await service.GetAllAsync()).ToList();
        Assert.Equal(2, all.Count);
        Assert.Contains(all, r => r.Name == "A");
        Assert.Contains(all, r => r.Name == "B");
    }

    [Fact]
    public async Task LaunchPadService_returns_all_pads()
    {
        var service = new InMemoryLaunchPadService();
        await service.CreateOrUpdateAsync(new LaunchPad { LaunchPadId = Guid.NewGuid(), PadName = "P1" });
        await service.CreateOrUpdateAsync(new LaunchPad { LaunchPadId = Guid.NewGuid(), PadName = "P2" });

        var all = (await service.GetAllAsync()).ToList();
        Assert.Equal(2, all.Count);
        Assert.Contains(all, p => p.PadName == "P1");
        Assert.Contains(all, p => p.PadName == "P2");
    }
}
