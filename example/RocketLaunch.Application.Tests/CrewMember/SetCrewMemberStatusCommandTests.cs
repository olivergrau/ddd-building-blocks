using System.Diagnostics;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command;
using RocketLaunch.Application.Command.CrewMember;
using RocketLaunch.Application.Command.CrewMember.Handler;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Application.Tests.CrewMember;

public class SetCrewMemberStatusCommandTests
{
    [Fact]
    public async Task Handle_SetCrewMemberStatusCommand()
    {
        var store = new PureInMemoryEventStorageProvider();
        var repository = new EventSourcingRepository(store);
        var registerHandler = new RegisterCrewMemberCommandHandler(repository);
        var registerCommand = new RegisterCrewMemberCommand(
            crewMemberId: Guid.NewGuid(),
            name: "Dave",
            role: CrewRole.Pilot,
            certifications: []
        );
        await registerHandler.HandleCommandAsync(registerCommand);

        var handler = new SetCrewMemberStatusCommandHandler(repository);
        await handler.HandleCommandAsync(new SetCrewMemberStatusCommand(registerCommand.CrewMemberId, CrewMemberStatus.Unavailable));

        var crew = await repository.GetByIdAsync<Domain.Model.CrewMember, CrewMemberId>(new CrewMemberId(registerCommand.CrewMemberId));
        Debug.Assert(crew != null);
        Assert.Equal(CrewMemberStatus.Unavailable, crew.Status);
        Assert.Equal(1, crew.CurrentVersion);
    }
}
