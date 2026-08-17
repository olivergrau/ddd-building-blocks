using RocketLaunch.SharedKernel.Events;
using System.Diagnostics;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command.CrewMember;
using RocketLaunch.Application.Command.CrewMember.Handler;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Application.Tests.CrewMember;

public class AssignCrewMemberCommandTests
{
    [Fact]
    public async Task Handle_AssignCrewMemberCommand()
    {
        var store = new InMemoryEventStoreProvider();
        var repository = new EventSourcingRepository(store, RocketLaunchEventCodec.Create());
        var registerHandler = new RegisterCrewMemberCommandHandler(repository);
        var registerCommand = new RegisterCrewMemberCommand(
            crewMemberId: Guid.NewGuid(),
            name: "Bob",
            role: CrewRole.Pilot,
            certifications: []
        );
        await registerHandler.HandleAsync(registerCommand, System.Threading.CancellationToken.None);

        var handler = new AssignCrewMemberCommandHandler(repository);
        await handler.HandleAsync(new AssignCrewMemberCommand(registerCommand.CrewMemberId), System.Threading.CancellationToken.None);

        var crew = await repository.GetByIdAsync<Domain.Model.CrewMember, CrewMemberId>(new CrewMemberId(registerCommand.CrewMemberId), System.Threading.CancellationToken.None);
        Debug.Assert(crew != null);
        Assert.Equal(CrewMemberStatus.Assigned, crew.Status);
        Assert.Equal(1, crew.CurrentVersion);
    }
}
