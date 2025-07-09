using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command;
using RocketLaunch.Application.Command.Handler;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Application.Tests.CrewMember;

public class RegisterCrewMemberCommandTests
{
    [Fact]
    public async Task Handle_RegisterCrewMemberCommand()
    {
        var store = new PureInMemoryEventStorageProvider();
        var repository = new EventSourcingRepository(store);
        var handler = new RegisterCrewMemberCommandHandler(repository);

        var command = new RegisterCrewMemberCommand(
            crewMemberId: Guid.NewGuid(),
            name: "Alice",
            role: CrewRole.Commander,
            certifications: ["Flight", "EVA"]
        );

        await handler.HandleCommandAsync(command);

        var crewMember = await repository.GetByIdAsync<Domain.Model.CrewMember, CrewMemberId>(new CrewMemberId(command.CrewMemberId));

        Assert.NotNull(crewMember);
        Assert.Equal(0, crewMember.CurrentVersion);
        Assert.Equal(command.CrewMemberId, crewMember.Id.Value);
        Assert.Equal(command.Name, crewMember.Name);
        Assert.Equal(command.Role, crewMember.Role);
        Assert.Equal(command.Certifications, crewMember.Certifications);
        Assert.Equal(CrewMemberStatus.Available, crewMember.Status);
    }
}
