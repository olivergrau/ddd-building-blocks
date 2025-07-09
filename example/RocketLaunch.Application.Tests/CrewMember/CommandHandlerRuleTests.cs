using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command;
using RocketLaunch.Application.Command.Handler;
using RocketLaunch.Domain.Model;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Application.Tests;

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
