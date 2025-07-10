using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using RocketLaunch.Application.Command;
using RocketLaunch.Application.Command.Handler;
using RocketLaunch.Application.Command.Mission.Handler;
using RocketLaunch.Application.Dto;
using RocketLaunch.Domain.Service;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.Application.Tests.Mocks;
using Xunit;

namespace RocketLaunch.Application.Tests.Mission;

public class CommandHandlerRuleTests
{
    private static async Task<(Guid missionId, IEventSourcingRepository repo, StubResourceAvailabilityService validator)>
        SetupMissionAsync()
    {
        var validator = new StubResourceAvailabilityService();
        var store = new PureInMemoryEventStorageProvider();
        var repository = new EventSourcingRepository(store);
        var registerHandler = new RegisterMissionCommandHandler(repository);
        var command = new RegisterMissionCommand(
            missionId: Guid.NewGuid(),
            missionName: "Apollo 11",
            targetOrbit: "Moon",
            payloadDescription: "Rover",
            launchWindow: new LaunchWindowDto(DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromDays(6))
        );
        await registerHandler.HandleCommandAsync(command);
        return (command.MissionId, repository, validator);
    }

    [Fact]
    public async Task AssignRocket_RocketNotAvailable_Throws()
    {
        var (missionId, repo, validator) = await SetupMissionAsync();
        validator.RocketIsAvailable = false;

        var handler = new AssignRocketCommandHandler(repo, validator);
        var command = new AssignRocketCommand(missionId, Guid.NewGuid());

        await Assert.ThrowsAsync<RuleValidationException>(() => handler.HandleCommandAsync(command));
    }

    [Fact]
    public async Task AssignLaunchPad_WithoutRocket_Throws()
    {
        var (missionId, repo, validator) = await SetupMissionAsync();
        var handler = new AssignLaunchPadCommandHandler(repo, validator);
        var command = new AssignLaunchPadCommand(missionId, Guid.NewGuid());

        await Assert.ThrowsAsync<AggregateValidationException>(() => handler.HandleCommandAsync(command));
    }

    [Fact]
    public async Task AssignLaunchPad_PadNotAvailable_Throws()
    {
        var (missionId, repo, validator) = await SetupMissionAsync();

        var rocketHandler = new AssignRocketCommandHandler(repo, validator);
        await rocketHandler.HandleCommandAsync(new AssignRocketCommand(missionId, Guid.NewGuid()));

        validator.LaunchPadIsAvailable = false;
        var handler = new AssignLaunchPadCommandHandler(repo, validator);
        var command = new AssignLaunchPadCommand(missionId, Guid.NewGuid());

        await Assert.ThrowsAsync<RuleValidationException>(() => handler.HandleCommandAsync(command));
    }

    [Fact]
    public async Task AssignCrew_WithoutRocketOrPad_Throws()
    {
        var (missionId, repo, validator) = await SetupMissionAsync();
        var crewAssignment = new CrewAssignment(validator);
        var handler = new AssignCrewCommandHandler(repo, crewAssignment);
        var crewId = Guid.NewGuid();
        var register = new RegisterCrewMemberCommandHandler(repo);
        await register.HandleCommandAsync(new RegisterCrewMemberCommand(
            crewId,
            "Alice",
            CrewRole.Commander,
            []));
        var command = new AssignCrewCommand(missionId, [crewId]);

        await Assert.ThrowsAsync<AggregateValidationException>(() => handler.HandleCommandAsync(command));
    }

    [Fact]
    public async Task AssignCrew_CrewNotAvailable_Throws()
    {
        var (missionId, repo, validator) = await SetupMissionAsync();
        var rocketHandler = new AssignRocketCommandHandler(repo, validator);
        await rocketHandler.HandleCommandAsync(new AssignRocketCommand(missionId, Guid.NewGuid()));
        var padHandler = new AssignLaunchPadCommandHandler(repo, validator);
        await padHandler.HandleCommandAsync(new AssignLaunchPadCommand(missionId, Guid.NewGuid()));

        validator.CrewIsAvailable = false;
        var crewAssignment = new CrewAssignment(validator);
        var handler = new AssignCrewCommandHandler(repo, crewAssignment);
        var crewId = Guid.NewGuid();
        var register = new RegisterCrewMemberCommandHandler(repo);
        await register.HandleCommandAsync(new RegisterCrewMemberCommand(
            crewId,
            "Bob",
            CrewRole.FlightEngineer,
            []));
        var command = new AssignCrewCommand(missionId, [crewId]);

        await Assert.ThrowsAsync<RuleValidationException>(() => handler.HandleCommandAsync(command));
    }

    [Fact]
    public async Task ScheduleMission_WithoutResources_Throws()
    {
        var (missionId, repo, _) = await SetupMissionAsync();
        var handler = new ScheduleMissionCommandHandler(repo);
        var command = new ScheduleMissionCommand(missionId);

        await Assert.ThrowsAsync<DDD.BuildingBlocks.Core.Exception.AggregateException>(() => handler.HandleCommandAsync(command));
    }

    [Fact]
    public async Task LaunchMission_NotScheduled_Throws()
    {
        var (missionId, repo, validator) = await SetupMissionAsync();
        var rocketHandler = new AssignRocketCommandHandler(repo, validator);
        await rocketHandler.HandleCommandAsync(new AssignRocketCommand(missionId, Guid.NewGuid()));
        var padHandler = new AssignLaunchPadCommandHandler(repo, validator);
        await padHandler.HandleCommandAsync(new AssignLaunchPadCommand(missionId, Guid.NewGuid()));

        var handler = new LaunchMissionCommandHandler(repo);
        var command = new LaunchMissionCommand(missionId);

        await Assert.ThrowsAsync<DDD.BuildingBlocks.Core.Exception.AggregateException>(() => handler.HandleCommandAsync(command));
    }

    [Fact]
    public async Task AbortMission_AfterLaunch_Throws()
    {
        var (missionId, repo, validator) = await SetupMissionAsync();
        var rocketHandler = new AssignRocketCommandHandler(repo, validator);
        await rocketHandler.HandleCommandAsync(new AssignRocketCommand(missionId, Guid.NewGuid()));
        var padHandler = new AssignLaunchPadCommandHandler(repo, validator);
        await padHandler.HandleCommandAsync(new AssignLaunchPadCommand(missionId, Guid.NewGuid()));
        var scheduleHandler = new ScheduleMissionCommandHandler(repo);
        await scheduleHandler.HandleCommandAsync(new ScheduleMissionCommand(missionId));
        var launchHandler = new LaunchMissionCommandHandler(repo);
        await launchHandler.HandleCommandAsync(new LaunchMissionCommand(missionId));

        var handler = new AbortMissionCommandHandler(repo);
        var command = new AbortMissionCommand(missionId);

        await Assert.ThrowsAsync<DDD.BuildingBlocks.Core.Exception.AggregateException>(() => handler.HandleCommandAsync(command));
    }

    [Fact]
    public async Task MarkMissionArrived_NotLaunched_Throws()
    {
        var (missionId, repo, validator) = await SetupMissionAsync();
        var rocketHandler = new AssignRocketCommandHandler(repo, validator);
        await rocketHandler.HandleCommandAsync(new AssignRocketCommand(missionId, Guid.NewGuid()));
        var padHandler = new AssignLaunchPadCommandHandler(repo, validator);
        await padHandler.HandleCommandAsync(new AssignLaunchPadCommand(missionId, Guid.NewGuid()));
        var scheduleHandler = new ScheduleMissionCommandHandler(repo);
        await scheduleHandler.HandleCommandAsync(new ScheduleMissionCommand(missionId));

        var handler = new MarkMissionArrivedCommandHandler(repo);
        var command = new MarkMissionArrivedCommand(
            missionId,
            DateTime.UtcNow,
            "Starship",
            [],
            []
        );

        await Assert.ThrowsAsync<DDD.BuildingBlocks.Core.Exception.AggregateException>(() => handler.HandleCommandAsync(command));
    }
}
