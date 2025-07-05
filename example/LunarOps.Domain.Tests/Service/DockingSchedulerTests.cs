// Tests/Domain/Service/DockingSchedulerTests.cs

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
    public class DockingSchedulerTests
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
            int portCount = 1,
            int maxCrew = 2,
            double maxPayload = 100)
        {
            var ports = Enumerable.Range(1, portCount)
                .Select(_ => new DockingPort(new DockingPortId(Guid.NewGuid())))
                .ToList();

            return new MoonStation(
                stationId,
                name: "Horizon",
                location: "EquatorialOrbit",
                status: StationStatus.Active,
                supportedVehicleTypes: [_vehicleType ?? throw new InvalidOperationException()],
                maxCrewCapacity: maxCrew,
                maxPayloadCapacity: maxPayload,
                dockingPorts: ports
            );
        }

        private LunarMission CreateMission(IStationAvailabilityService validator, StationId assignStation)
            => new(
                _missionId,
                _arrivalTime,
                _vehicleType,
                _crewManifest,
                _payloadManifest,
                assignStation,
                validator
            );

        [Fact]
        public async Task ScheduleDocking_HappyPath_ReservesPortAndAssigns()
        {
            // Arrange
            var stub    = new StubStationAvailabilityService();
            var station = CreateStation(_stationId, portCount: 1);
            var mission = CreateMission(stub, _stationId);
            var scheduler = new DockingScheduler(stub);

            // Act
            await scheduler.ScheduleDockingAsync(mission, station);

            // Assert mission status
            Assert.Equal(LunarMissionStatus.DockingScheduled, mission.Status);
            Assert.NotNull(mission.AssignedPort);

            // Assert station port occupied by mission.VehicleType
            var port = station.DockingPorts.Single();
            Assert.Equal(DockingPortStatus.Occupied, port.Status);
            Assert.Equal(_vehicleType, port.AssignedVehicle);
        }

        [Fact]
        public async Task ScheduleDocking_StationMismatch_ThrowsInvalidOperation()
        {
            // Arrange
            var stub    = new StubStationAvailabilityService();
            var station = CreateStation(_stationId);
            var otherId = new StationId(Guid.NewGuid());
            var mission = CreateMission(stub, otherId);
            var scheduler = new DockingScheduler(stub);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => scheduler.ScheduleDockingAsync(mission, station)
            );
        }

        [Fact]
        public async Task ScheduleDocking_NoFreePort_ThrowsRuleValidationException()
        {
            // Arrange
            var stub    = new StubStationAvailabilityService { HasFreePort = true };
            var station = CreateStation(_stationId);
            var mission = CreateMission(stub, _stationId);
            var scheduler = new DockingScheduler(stub);
            
            stub.HasFreePort = false;
            
            // Act & Assert
            var ex = await Assert.ThrowsAsync<RuleValidationException>(
                () => scheduler.ScheduleDockingAsync(mission, station)
            );
            Assert.Contains("no available docking port", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ScheduleDocking_NotEnoughCrewCapacity_ThrowsRuleValidationException()
        {
            // Arrange
            var stub    = new StubStationAvailabilityService { HasCrewCapacity = true, HasFreePort = true, HasStorage = true};
            var station = CreateStation(_stationId);
            var mission = CreateMission(stub, _stationId);
            var scheduler = new DockingScheduler(stub);
            
            stub.HasFreePort = true;
            stub.HasCrewCapacity = false;
            
            // Act & Assert
            var ex = await Assert.ThrowsAsync<RuleValidationException>(
                () => scheduler.ScheduleDockingAsync(mission, station)
            );
            Assert.Contains("no remaining crew capacity", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ScheduleDocking_NotEnoughStorageCapacity_ThrowsRuleValidationException()
        {
            // Arrange
            var stub    = new StubStationAvailabilityService { HasCrewCapacity = true, HasFreePort = true, HasStorage = true};
            var station = CreateStation(_stationId);
            var mission = CreateMission(stub, _stationId);
            var scheduler = new DockingScheduler(stub);
            
            stub.HasFreePort = true;
            stub.HasCrewCapacity = true;
            stub.HasStorage = false;
            
            // Act & Assert
            var ex = await Assert.ThrowsAsync<RuleValidationException>(
                () => scheduler.ScheduleDockingAsync(mission, station)
            );
            Assert.Contains("no remaining payload capacity", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
