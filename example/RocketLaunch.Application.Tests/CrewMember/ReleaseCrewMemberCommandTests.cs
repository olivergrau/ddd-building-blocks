using System.Diagnostics;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command;
using RocketLaunch.Application.Command.Handler;
using RocketLaunch.Domain.Model;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Application.Tests;

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
        await registerHandler.HandleCommandAsync(registerCommand);

        var assignHandler = new AssignCrewMemberCommandHandler(repository);
        await assignHandler.HandleCommandAsync(new AssignCrewMemberCommand(registerCommand.CrewMemberId));

        var releaseHandler = new ReleaseCrewMemberCommandHandler(repository);
        await releaseHandler.HandleCommandAsync(new ReleaseCrewMemberCommand(registerCommand.CrewMemberId));

        var crew = await repository.GetByIdAsync<CrewMember, CrewMemberId>(new CrewMemberId(registerCommand.CrewMemberId));
        Debug.Assert(crew != null);
        Assert.Equal(CrewMemberStatus.Available, crew.Status);
        Assert.Equal(2, crew.CurrentVersion);
    }
}
