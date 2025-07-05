// Tests/Domain/Service/UndockingManeuverTests.cs

using System.Reflection;
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
    public class UndockingManeuverTests
    {
        private readonly ExternalMissionId _missionId = new(Guid.NewGuid().ToString());
        private readonly StationId _stationId = new(Guid.NewGuid());
        private readonly DateTime _arrivalTime = DateTime.UtcNow;
        private readonly VehicleType _vehicleType = new("Starship");
        private readonly (string, string)[] _crewManifest =
            [("Alice", "Commander"), ("Bob", "Scientist")];
        private readonly (string, double)[] _payloadManifest =
            [("Rover", 10.0), ("Supplies", 20.0)];

        private MoonStation CreateStation()
        {
            var ports = new[] { new DockingPort(new DockingPortId(Guid.NewGuid())) };
            return new MoonStation(
                _stationId,
                name: "Alpha",
                location: "LunarOrbit",
                status: StationStatus.Active,
                supportedVehicleTypes: [_vehicleType],
                maxCrewCapacity: 5,
                maxPayloadCapacity: 100,
                dockingPorts: ports
            );
        }

        private LunarMission RegisterAndDockMission(out MoonStation station)
        {
            var validator = new StubStationAvailabilityService();
            station = CreateStation();
            var mission = new LunarMission(
                _missionId,
                _arrivalTime,
                _vehicleType,
                _crewManifest,
                _payloadManifest,
                _stationId,
                validator
            );

            // Schedule docking via service
            var scheduler = new DockingScheduler(validator);
            scheduler.ScheduleDockingAsync(mission, station).GetAwaiter().GetResult();
            mission.CompleteDocking();
            mission.UnloadPayload();
            mission.TransferCrew(mission.CrewManifest
                                  .Select(_ => new LunarCrewMemberId(Guid.NewGuid())));
            mission.MarkInService();

            return mission;
        }

        [Fact]
        public void Undock_HappyPath_ReleasesPortAndDeparts()
        {
            // Arrange
            var maneuver = new UndockingManeuver();
            var mission = RegisterAndDockMission(out var station);
            var portId = mission.AssignedPort;

            // Act
            maneuver.Undock(mission, station);

            // Assert mission departed
            Assert.Equal(LunarMissionStatus.Departed, mission.Status);

            // Assert station port freed
            var port = station.DockingPorts.Single(p => p.Id == portId);
            Assert.Equal(DockingPortStatus.Available, port.Status);
            Assert.Null(port.AssignedVehicle);
        }

        [Fact]
        public void Undock_NotInService_ThrowsAggregateValidationException()
        {
            // Arrange
            var validator = new StubStationAvailabilityService();
            var station = CreateStation();
            var mission = new LunarMission(
                _missionId,
                _arrivalTime,
                _vehicleType,
                _crewManifest,
                _payloadManifest,
                _stationId,
                validator
            );
            // mission.Status == Registered

            var maneuver = new UndockingManeuver();

            // Act & Assert
            var ex = Assert.Throws<AggregateValidationException>(
                () => maneuver.Undock(mission, station)
            );
            Assert.Contains("in-service", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Undock_InServiceButNoPort_ThrowsAggregateValidationException()
        {
            // Arrange
            var mission = RegisterAndDockMission(out var station);
            // Clear AssignedPort via reflection
            var prop = typeof(LunarMission).GetProperty(
                "AssignedPort",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            Assert.NotNull(prop);
            prop.SetValue(mission, null);

            var maneuver = new UndockingManeuver();

            // Act & Assert
            var ex = Assert.Throws<AggregateValidationException>(
                () => maneuver.Undock(mission, station)
            );
            Assert.Contains("No docking port assigned", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
