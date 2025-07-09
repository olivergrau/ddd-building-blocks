using System.Diagnostics;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command;
using RocketLaunch.Application.Command.Handler;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Application.Tests.CrewMember;

public class AssignCrewMemberCommandTests
{
    [Fact]
    public async Task Handle_AssignCrewMemberCommand()
    {
        var store = new PureInMemoryEventStorageProvider();
        var repository = new EventSourcingRepository(store);
        var registerHandler = new RegisterCrewMemberCommandHandler(repository);
        var registerCommand = new RegisterCrewMemberCommand(
            crewMemberId: Guid.NewGuid(),
            name: "Bob",
            role: CrewRole.Pilot,
            certifications: []
        );
        await registerHandler.HandleCommandAsync(registerCommand);

        var handler = new AssignCrewMemberCommandHandler(repository);
        await handler.HandleCommandAsync(new AssignCrewMemberCommand(registerCommand.CrewMemberId));

        var crew = await repository.GetByIdAsync<Domain.Model.CrewMember, CrewMemberId>(new CrewMemberId(registerCommand.CrewMemberId));
        Debug.Assert(crew != null);
        Assert.Equal(CrewMemberStatus.Assigned, crew.Status);
        Assert.Equal(1, crew.CurrentVersion);
    }
}
