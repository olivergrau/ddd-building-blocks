using Microsoft.Extensions.Logging.Abstractions;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Projector;
using RocketLaunch.ReadModel.Core.Projector.Mission;
using RocketLaunch.ReadModel.InMemory.Service;
using RocketLaunch.SharedKernel.Events.Mission;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.ReadModel.Tests;

public class CrewMemberProjectorTests
{
    [Fact]
    public async Task CrewAssigned_marks_members_assigned()
    {
        var service = new InMemoryCrewService();
        var projector = new CrewMemberProjector(service, NullLogger<CrewMemberProjector>.Instance);

        var memberId = Guid.NewGuid();
        await service.CreateOrUpdateAsync(new CrewMember
        {
            CrewMemberId = memberId,
            Name = "Alice",
            Role = "Commander",
            Status = CrewMemberStatus.Available
        });

        var missionId = Guid.NewGuid();
        await projector.WhenAsync(new CrewAssigned(new MissionId(missionId), [new CrewMemberId(memberId)]));

        var member = service.GetById(memberId)
            ?? throw new InvalidOperationException("Crew member not found after assignment");
        
        Assert.Equal(CrewMemberStatus.Assigned, member.Status);
        Assert.Equal(missionId, member.AssignedMissionId);
    }

    [Fact]
    public async Task MissionAborted_releases_crew_members()
    {
        var service = new InMemoryCrewService();
        var projector = new CrewMemberProjector(service, NullLogger<CrewMemberProjector>.Instance);

        var memberId = Guid.NewGuid();
        await service.CreateOrUpdateAsync(new CrewMember
        {
            CrewMemberId = memberId,
            Name = "Bob",
            Role = "Pilot",
            Status = CrewMemberStatus.Assigned,
            AssignedMissionId = Guid.NewGuid()
        });
        var missionId = service.GetById(memberId)!.AssignedMissionId!.Value;

        await projector.WhenAsync(new MissionAborted(new MissionId(missionId)));

        var member = service.GetById(memberId)!;
        Assert.Equal(CrewMemberStatus.Available, member.Status);
        Assert.Null(member.AssignedMissionId);
    }
}
