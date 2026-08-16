using System.Diagnostics;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command.CrewMember;
using RocketLaunch.Application.Command.CrewMember.Handler;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Application.Tests.CrewMember;

public class ReleaseCrewMemberCommandTests
{
    [Fact]
    public async Task Handle_ReleaseCrewMemberCommand()
    {
        var store = new PureInMemoryEventStorageProvider();
        var repository = new EventSourcingRepository(store);
        var registerHandler = new RegisterCrewMemberCommandHandler(repository);
        var registerCommand = new RegisterCrewMemberCommand(
            crewMemberId: Guid.NewGuid(),
            name: "Eve",
            role: CrewRole.MissionSpecialist,
            certifications: []
        );
        await registerHandler.HandleAsync(registerCommand, System.Threading.CancellationToken.None);

        var assignHandler = new AssignCrewMemberCommandHandler(repository);
        await assignHandler.HandleAsync(new AssignCrewMemberCommand(registerCommand.CrewMemberId), System.Threading.CancellationToken.None);

        var releaseHandler = new ReleaseCrewMemberCommandHandler(repository);
        await releaseHandler.HandleAsync(new ReleaseCrewMemberCommand(registerCommand.CrewMemberId), System.Threading.CancellationToken.None);

        var crew = await repository.GetByIdAsync<Domain.Model.CrewMember, CrewMemberId>(new CrewMemberId(registerCommand.CrewMemberId), System.Threading.CancellationToken.None);
        Debug.Assert(crew != null);
        Assert.Equal(CrewMemberStatus.Available, crew.Status);
        Assert.Equal(2, crew.CurrentVersion);
    }
}
