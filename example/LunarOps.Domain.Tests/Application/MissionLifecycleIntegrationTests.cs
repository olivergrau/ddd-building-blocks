// Tests/Domain/Service/MissionLifecycleIntegrationTests.cs

using LunarOps.Domain.Model;
using LunarOps.Domain.Model.Entities;
using LunarOps.Domain.Service;
using LunarOps.SharedKernel.Enums;
using LunarOps.SharedKernel.ValueObjects;
using Xunit;

namespace LunarOps.Domain.Tests.Application
{
    public class MissionLifecycleIntegrationTests
    {
        // A station‐validator stub that always returns “ok”
        private class AlwaysValidStationAvailabilityService : IStationAvailabilityService
        {
            public Task<bool> HasFreePortAsync(StationId stationId) 
                => Task.FromResult(true);
            public Task<bool> HasCrewCapacityAsync(StationId stationId, int crewCount) 
                => Task.FromResult(true);
            public Task<bool> HasStorageCapacityAsync(StationId stationId, double payloadMass) 
                => Task.FromResult(true);
            public Task<bool> HasSupportedVehicleTypeAsync(StationId stationId, VehicleType vehicleType) 
                => Task.FromResult(true);
        }
        
        private readonly VehicleType _vehicleType = new("Starship");
        
        private MoonStation CreateStation(StationId id)
        {
            var ports = new[] { new DockingPort(new DockingPortId(Guid.NewGuid())) };
            return new MoonStation(
                id,
                name: "Alpha",
                location: "EquatorialOrbit",
                status: StationStatus.Active,
                supportedVehicleTypes: [_vehicleType],
                maxCrewCapacity: 5,
                maxPayloadCapacity: 100,
                dockingPorts: ports
            );
        }

        private LunarMission RegisterMission(
            StationId stationId,
            IStationAvailabilityService validator)
        {
            var crew = new[] { ("Alice", "Commander"), ("Bob", "Scientist") };
            var payload = new[] { ("Rover", 10.0), ("Supplies", 20.0) };

            return new LunarMission(
                new ExternalMissionId(Guid.NewGuid().ToString()),
                DateTime.UtcNow,
                _vehicleType,
                crew,
                payload,
                stationId,
                validator
            );
        }

        [Fact]
        public async Task HappyPath_UnloadThenCrew_CompletesLifecycle()
        {
            // Arrange
            var validator = new AlwaysValidStationAvailabilityService();
            var stationId = new StationId(Guid.NewGuid());
            var station = CreateStation(stationId);
            var mission = RegisterMission(stationId, validator);

            var dockingScheduler = new DockingScheduler(validator);
            var payloadHandler   = new PayloadHandling();
            var crewTransfer     = new CrewMemberTransfer();
            var undocking        = new UndockingManeuver();

            // Act
            await dockingScheduler.ScheduleDockingAsync(mission, station);
            mission.CompleteDocking();

            await payloadHandler.UnloadAsync(mission, station, validator);
            await crewTransfer.TransferCrew(mission, station, validator);

            mission.MarkInService();
            undocking.Undock(mission, station);

            // Assert
            Assert.Equal(LunarMissionStatus.Departed, mission.Status);
            var port = station.DockingPorts.Single();
            Assert.Equal(DockingPortStatus.Available, port.Status);
        }

        [Fact]
        public async Task HappyPath_CrewThenUnload_CompletesLifecycle()
        {
            // Arrange
            var validator = new AlwaysValidStationAvailabilityService();
            var stationId = new StationId(Guid.NewGuid());
            var station = CreateStation(stationId);
            var mission = RegisterMission(stationId, validator);

            var dockingScheduler = new DockingScheduler(validator);
            var payloadHandler   = new PayloadHandling();
            var crewTransfer     = new CrewMemberTransfer();
            var undocking        = new UndockingManeuver();

            // Act
            await dockingScheduler.ScheduleDockingAsync(mission, station);
            mission.CompleteDocking();

            await crewTransfer.TransferCrew(mission, station, validator);
            await payloadHandler.UnloadAsync(mission, station, validator);

            mission.MarkInService();
            undocking.Undock(mission, station);

            // Assert
            Assert.Equal(LunarMissionStatus.Departed, mission.Status);
            var port = station.DockingPorts.Single();
            Assert.Equal(DockingPortStatus.Available, port.Status);
        }
    }
}
