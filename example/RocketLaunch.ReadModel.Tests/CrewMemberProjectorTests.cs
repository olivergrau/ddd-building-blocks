using Microsoft.Extensions.Logging.Abstractions;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Projector.CrewMember;
using RocketLaunch.ReadModel.InMemory.Service;
using RocketLaunch.ReadModel.Core.Service;
using RocketLaunch.ReadModel.Core.Exceptions;
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

        var member = (await service.GetByIdAsync(memberId))!;
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

        var member = (await service.GetByIdAsync(memberId))!;
        Assert.Equal(CrewMemberStatus.Assigned, member.Status);
    }

    [Fact]
    public async Task CreateOrUpdate_failure_throws_service_exception()
    {
        var projector = new CrewMemberProjector(new FailingCrewService(), NullLogger<CrewMemberProjector>.Instance);

        await Assert.ThrowsAsync<ReadModelServiceException>(() =>
            projector.WhenAsync(
                new CrewMemberRegistered(
                    new CrewMemberId(Guid.NewGuid()),
                    "X",
                    SharedKernel.Enums.CrewRole.Commander,
                    ["A"])));
    }

    private class FailingCrewService : ICrewMemberService
    {
        public Task<CrewMember?> GetByIdAsync(Guid id) => Task.FromResult<CrewMember?>(null);
        public Task<IEnumerable<CrewMember>> GetAllAsync() => Task.FromResult<IEnumerable<CrewMember>>(Array.Empty<CrewMember>());
        public Task<bool> IsAvailableAsync(Guid crewMemberId, string requiredRole) => Task.FromResult(false);
        public Task<IEnumerable<CrewMember>> FindByAssignedMissionAsync(Guid missionId) => Task.FromResult<IEnumerable<CrewMember>>(Array.Empty<CrewMember>());
        public Task<IEnumerable<CrewMember>> FindAvailableAsync(string role, string? certification = null) => Task.FromResult<IEnumerable<CrewMember>>(Array.Empty<CrewMember>());
        public Task CreateOrUpdateAsync(CrewMember member) => throw new Exception("fail");
    }
}
