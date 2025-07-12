using System.Diagnostics;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command.CrewMember;
using RocketLaunch.Application.Command.CrewMember.Handler;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Application.Tests.CrewMember;

public class SetCrewMemberCertificationsCommandTests
{
    [Fact]
    public async Task Handle_SetCrewMemberCertificationsCommand()
    {
        var store = new PureInMemoryEventStorageProvider();
        var repository = new EventSourcingRepository(store);
        var registerHandler = new RegisterCrewMemberCommandHandler(repository);
        var registerCommand = new RegisterCrewMemberCommand(
            crewMemberId: Guid.NewGuid(),
            name: "Carol",
            role: CrewRole.FlightEngineer,
            certifications: ["A"]
        );
        await registerHandler.HandleCommandAsync(registerCommand);

        var handler = new SetCrewMemberCertificationsCommandHandler(repository);
        var newCerts = new[] { "B", "C" };
        await handler.HandleCommandAsync(new SetCrewMemberCertificationsCommand(registerCommand.CrewMemberId, newCerts));

        var crew = await repository.GetByIdAsync<Domain.Model.CrewMember, CrewMemberId>(new CrewMemberId(registerCommand.CrewMemberId));
        Debug.Assert(crew != null);
        Assert.Equal(newCerts, crew.Certifications);
        Assert.Equal(1, crew.CurrentVersion);
    }
}
