// Tests/Domain/Service/PayloadHandlingTests.cs

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
    public class PayloadHandlingTests
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
            double maxPayload = 100)
        {
            // ports/crew not relevant here
            var ports = new[] { new DockingPort(new DockingPortId(Guid.NewGuid())) };
            return new MoonStation(
                stationId,
                name: "Horizon",
                location: "EquatorialOrbit",
                status: StationStatus.Active,
                supportedVehicleTypes: [_vehicleType],
                maxCrewCapacity: 10,
                maxPayloadCapacity: maxPayload,
                dockingPorts: ports
            );
        }

        private LunarMission CreateMission(IStationAvailabilityService validator)
            => new(
                _missionId,
                _arrivalTime,
                _vehicleType,
                _crewManifest,
                _payloadManifest,
                _stationId,
                validator
            );

        [Fact]
        public async Task Unload_HappyPath_FromDocked_StoresAllPayloadAndUpdatesMission()
        {
            // Arrange
            var stub    = new StubStationAvailabilityService();
            var station = CreateStation(_stationId, maxPayload: 100);
            var mission = CreateMission(stub);

            // Move mission to Docked
            mission.AssignDockingPort(new DockingPortId(Guid.NewGuid()));
            mission.CompleteDocking();

            var service = new PayloadHandling();

            // Act
            await service.UnloadAsync(mission, station, stub);

            // Assert: all payload items moved to station
            Assert.Equal(_payloadManifest.Count, station.StoredPayloads.Count);
            foreach (var (item, mass) in _payloadManifest)
            {
                Assert.Contains(station.StoredPayloads, p =>
                    p.Description == item && Math.Abs(p.Mass - mass) < 1e-6);
            }

            // Assert: mission status transitioned
            Assert.Equal(LunarMissionStatus.PayloadUnloaded, mission.Status);
        }

        [Fact]
        public async Task Unload_HappyPath_FromCrewTransferred_StoresAllPayloadAndUpdatesMission()
        {
            // Arrange
            var stub    = new StubStationAvailabilityService();
            var station = CreateStation(_stationId, maxPayload: 100);
            var mission = CreateMission(stub);

            // Move mission to CrewTransferred
            mission.AssignDockingPort(new DockingPortId(Guid.NewGuid()));
            mission.CompleteDocking();
            mission.TransferCrew(_crewManifest.Select(_ => new LunarCrewMemberId(Guid.NewGuid())));

            var service = new PayloadHandling();

            // Act
            await service.UnloadAsync(mission, station, stub);

            // Assert
            Assert.Equal(_payloadManifest.Count, station.StoredPayloads.Count);
            Assert.Equal(LunarMissionStatus.ReadyForService, mission.Status);
        }

        [Fact]
        public async Task Unload_NotEnoughStorage_ThrowsRuleValidationException()
        {
            // Arrange
            var stub    = new StubStationAvailabilityService { HasStorage = true };
            var station = CreateStation(_stationId, maxPayload: 100);
            var mission = CreateMission(stub);

            mission.AssignDockingPort(new DockingPortId(Guid.NewGuid()));
            mission.CompleteDocking();

            var service = new PayloadHandling();
            
            stub.HasStorage = false;
            
            // Act & Assert
            await Assert.ThrowsAsync<RuleValidationException>(
                () => service.UnloadAsync(mission, station, stub)
            );
        }

        [Theory]
        [InlineData(LunarMissionStatus.Registered)]
        [InlineData(LunarMissionStatus.DockingScheduled)]
        [InlineData(LunarMissionStatus.InService)]
        [InlineData(LunarMissionStatus.Departed)]
        public async Task Unload_InvalidState_ThrowsAggregateValidationException(LunarMissionStatus state)
        {
            // Arrange
            var stub    = new StubStationAvailabilityService();
            var station = CreateStation(_stationId);
            var mission = CreateMission(stub);

            // Drive mission into the unwanted state via its public API
            switch (state)
            {
                case LunarMissionStatus.DockingScheduled:
                    mission.AssignDockingPort(new DockingPortId(Guid.NewGuid()));
                    break;
                case LunarMissionStatus.InService:
                    mission.AssignDockingPort(new DockingPortId(Guid.NewGuid()));
                    mission.CompleteDocking();
                    mission.UnloadPayload();
                    mission.TransferCrew(_crewManifest.Select(_ => new LunarCrewMemberId(Guid.NewGuid())));
                    mission.MarkInService();
                    break;
                case LunarMissionStatus.Departed:
                    mission.AssignDockingPort(new DockingPortId(Guid.NewGuid()));
                    mission.CompleteDocking();
                    mission.UnloadPayload();
                    mission.TransferCrew(_crewManifest.Select(_ => new LunarCrewMemberId(Guid.NewGuid())));
                    mission.MarkInService();
                    mission.Depart();
                    break;
                // Registered: do nothing
            }

            var service = new PayloadHandling();

            // Act & Assert
            await Assert.ThrowsAsync<AggregateValidationException>(
                () => service.UnloadAsync(mission, station, stub)
            );
        }
    }
}
