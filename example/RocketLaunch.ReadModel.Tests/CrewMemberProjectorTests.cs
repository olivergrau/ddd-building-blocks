using Microsoft.Extensions.Logging.Abstractions;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Projector.CrewMember;
using RocketLaunch.ReadModel.InMemory.Service;
using RocketLaunch.SharedKernel.Events.CrewMember;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.ReadModel.Tests;

public class CrewMemberProjectorTests
{
    [Fact]
    public async Task CrewMemberRegistered_creates_member()
    {
        var missionService = new InMemoryMissionService();
        var service = new InMemoryCrewService(missionService);
        var projector = new CrewMemberProjector(service, NullLogger<CrewMemberProjector>.Instance);

        var memberId = Guid.NewGuid();
        await projector.WhenAsync(new CrewMemberRegistered(new CrewMemberId(memberId), "Alice", SharedKernel.Enums.CrewRole.Commander, ["Flight"]));

        var member = service.GetById(memberId)!;
        Assert.Equal("Alice", member.Name);
        Assert.Equal(CrewMemberStatus.Available, member.Status);
    }

    [Fact]
    public async Task CrewMemberAssigned_sets_status_assigned()
    {
        var missionService = new InMemoryMissionService();
        var service = new InMemoryCrewService(missionService);
        var projector = new CrewMemberProjector(service, NullLogger<CrewMemberProjector>.Instance);

        var memberId = Guid.NewGuid();
        await service.CreateOrUpdateAsync(new CrewMember { CrewMemberId = memberId, Name = "Bob", Role = "Pilot", Status = CrewMemberStatus.Available });

        await projector.WhenAsync(new CrewMemberAssigned(new CrewMemberId(memberId)));

        var member = service.GetById(memberId)!;
        Assert.Equal(CrewMemberStatus.Assigned, member.Status);
    }
}
