using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command.CrewMember;
using RocketLaunch.Application.Command.CrewMember.Handler;
using RocketLaunch.SharedKernel.Enums;
using Xunit;

namespace RocketLaunch.Application.Tests.CrewMember;

public class CrewMemberCommandHandlerRuleTests
{
    [Fact]
    public async Task AssignCrewMember_WhenUnavailable_Throws()
    {
        var store = new PureInMemoryEventStorageProvider();
        var repository = new EventSourcingRepository(store);
        var registerHandler = new RegisterCrewMemberCommandHandler(repository);
        var command = new RegisterCrewMemberCommand(Guid.NewGuid(), "Zed", CrewRole.FlightEngineer, []);
        await registerHandler.HandleCommandAsync(command);

        var statusHandler = new SetCrewMemberStatusCommandHandler(repository);
        await statusHandler.HandleCommandAsync(new SetCrewMemberStatusCommand(command.CrewMemberId, CrewMemberStatus.Unavailable));

        var handler = new AssignCrewMemberCommandHandler(repository);
        await Assert.ThrowsAsync<Exception>(() => handler.HandleCommandAsync(new AssignCrewMemberCommand(command.CrewMemberId)));
    }
}
