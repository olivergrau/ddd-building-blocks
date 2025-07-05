// Tests/Domain/Service/CrewMemberTransferTests.cs

using DDD.BuildingBlocks.Core.Exception;
using LunarOps.Domain.Model;
using LunarOps.Domain.Model.Entities;
using LunarOps.Domain.Service;
using LunarOps.Domain.Tests.Mocks;
using LunarOps.SharedKernel.Enums;
using LunarOps.SharedKernel.ValueObjects;
using Xunit;

namespace LunarOps.Domain.Tests.Service
{
    public class CrewMemberTransferTests
    {
        private readonly ExternalMissionId _missionId = new(Guid.NewGuid().ToString());
        private readonly StationId _stationId = new(Guid.NewGuid());
        private readonly DateTime _arrivalTime = DateTime.UtcNow;
        private readonly VehicleType _vehicleType = new("Starship");
        private readonly List<(string Name, string Role)> _crewManifest =
            [("Alice", "Commander"), ("Bob", "Scientist")];
        private readonly List<(string Item, double Mass)> _payloadManifest =
            [("Rover", 10.0), ("Supplies", 20.0)];

        private MoonStation CreateStation(
            StationId stationId,
            int maxCrew = 10)
        {
            // ports/storage not relevant here
            var ports = new[] { new DockingPort(new DockingPortId(Guid.NewGuid())) };
            return new MoonStation(
                stationId,
                name: "Horizon",
                location: "EquatorialOrbit",
                status: StationStatus.Active,
                supportedVehicleTypes: [_vehicleType ?? throw new InvalidOperationException()],
                maxCrewCapacity: maxCrew,
                maxPayloadCapacity: 1000,
                dockingPorts: ports
            );
        }

        private LunarMission CreateMission(IStationAvailabilityService validator)
            => new LunarMission(
                _missionId,
                _arrivalTime,
                _vehicleType,
                _crewManifest,
                _payloadManifest,
                _stationId,
                validator
            );

        [Fact]
        public async Task TransferCrew_HappyPath_FromDocked_TransfersAll()
        {
            // Arrange
            var stub    = new StubStationAvailabilityService();
            var station = CreateStation(_stationId, maxCrew: 5);
            var mission = CreateMission(stub);

            // Move mission to Docked
            mission.AssignDockingPort(new DockingPortId(Guid.NewGuid()));
            mission.CompleteDocking();

            var service = new CrewMemberTransfer();

            // Act
            await service.TransferCrew(mission, station, stub);

            // Assert station got all crew
            Assert.Equal(_crewManifest.Count, station.CrewQuarters.Count);
            // Assert mission status advanced
            Assert.Equal(LunarMissionStatus.CrewTransferred, mission.Status);
        }

        [Fact]
        public async Task TransferCrew_HappyPath_FromPayloadUnloaded_TransfersAll()
        {
            // Arrange
            var stub    = new StubStationAvailabilityService();
            var station = CreateStation(_stationId, maxCrew: 5);
            var mission = CreateMission(stub);

            // Move mission to PayloadUnloaded
            mission.AssignDockingPort(new DockingPortId(Guid.NewGuid()));
            mission.CompleteDocking();
            mission.UnloadPayload();

            var service = new CrewMemberTransfer();

            // Act
            await service.TransferCrew(mission, station, stub);

            // Assert station got all crew
            Assert.Equal(_crewManifest.Count, station.CrewQuarters.Count);
            // Assert mission status advanced
            Assert.Equal(LunarMissionStatus.ReadyForService, mission.Status);
        }

        [Fact]
        public async Task TransferCrew_InsufficientCapacity_ThrowsRuleValidationException()
        {
            // Arrange
            var stub    = new StubStationAvailabilityService { HasCrewCapacity = true };
            var station = CreateStation(_stationId, maxCrew: 5);
            var mission = CreateMission(stub);
            mission.AssignDockingPort(new DockingPortId(Guid.NewGuid()));
            mission.CompleteDocking();

            var service = new CrewMemberTransfer();

            stub.HasCrewCapacity = false;
            
            // Act & Assert
            var ex = await Assert.ThrowsAsync<RuleValidationException>(
                () => service.TransferCrew(mission, station, stub)
            );
            Assert.Contains("sufficient crew capacity", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(LunarMissionStatus.Registered)]
        [InlineData(LunarMissionStatus.DockingScheduled)]
        [InlineData(LunarMissionStatus.InService)]
        [InlineData(LunarMissionStatus.Departed)]
        public async Task TransferCrew_InvalidState_ThrowsAggregateValidationException(LunarMissionStatus state)
        {
            // Arrange
            var stub = new StubStationAvailabilityService();
            var station = CreateStation(_stationId, maxCrew: 5);
            var mission = CreateMission(stub);
            
            // Drive mission into the unwanted state via its public API where possible
            switch (state)
            {
                case LunarMissionStatus.DockingScheduled:
                    mission.AssignDockingPort(new DockingPortId(Guid.NewGuid()));
                    break;
                case LunarMissionStatus.InService:
                    mission.AssignDockingPort(new DockingPortId(Guid.NewGuid()));
                    mission.CompleteDocking();
                    mission.UnloadPayload();
                    var ids = station.CrewQuarters.Select(c => c.Id).ToList();
                    mission.TransferCrew(ids);
                    mission.MarkInService();
                    break;
                case LunarMissionStatus.Departed:
                    mission.AssignDockingPort(new DockingPortId(Guid.NewGuid()));
                    mission.CompleteDocking();
                    mission.UnloadPayload();
                    var crewIds = station.CrewQuarters.Select(c => c.Id).ToList();
                    mission.TransferCrew(crewIds);
                    mission.MarkInService();
                    mission.Depart();
                    break;
            }

            // Act & Assert
            await Assert.ThrowsAsync<AggregateValidationException>(
                () => new CrewMemberTransfer().TransferCrew(mission, station, stub)
            );
        }
    }
}
