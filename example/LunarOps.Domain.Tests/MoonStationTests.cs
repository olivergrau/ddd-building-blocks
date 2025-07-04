// Tests/Domain/Aggregates/MoonStationTests.cs

using DDD.BuildingBlocks.Core.Exception;
using LunarOps.Domain.Model;
using LunarOps.Domain.Model.Entities;
using LunarOps.SharedKernel.Enums;
using LunarOps.SharedKernel.ValueObjects;
using Xunit;

namespace LunarOps.Domain.Tests
{
    public class MoonStationTests
    {
        private readonly StationId _stationId = new StationId(Guid.NewGuid());
        private readonly ExternalMissionId _missionId = new ExternalMissionId(Guid.NewGuid().ToString());
        private const string SupportedVehicle = "Starship";
        private const string UnsupportedVehicle = "Capsule";

        private MoonStation CreateStation(
            int portCount = 2,
            int maxCrew = 2,
            double maxPayload = 100
        )
        {
            var ports = Enumerable.Range(1, portCount)
                .Select(_ => new DockingPort(new DockingPortId(Guid.NewGuid())))
                .ToList();

            return new MoonStation(
                _stationId,
                name: "Alpha",
                location: "SouthPole",
                status: StationStatus.Active,
                supportedVehicleTypes: new[] { SupportedVehicle },
                maxCrewCapacity: maxCrew,
                maxPayloadCapacity: maxPayload,
                dockingPorts: ports
            );
        }

        [Fact]
        public void ReserveDockingPort_SucceedsAndOccupiesPort()
        {
            var station = CreateStation(portCount: 1);
            // Act
            station.ReserveDockingPort(_missionId, SupportedVehicle);

            // Assert: single port is now occupied
            var port = station.DockingPorts.Single();
            Assert.Equal(DockingPortStatus.Occupied, port.Status);
            Assert.Equal(SupportedVehicle, port.AssignedVehicle);
        }

        [Fact]
        public void ReserveDockingPort_UnsupportedVehicle_Throws()
        {
            var station = CreateStation();
            var ex = Assert.Throws<AggregateValidationException>(
                () => station.ReserveDockingPort(_missionId, UnsupportedVehicle)
            );
            Assert.Contains("Vehicle type not supported", ex.Message);
        }

        [Fact]
        public void ReserveDockingPort_NoPortsAvailable_Throws()
        {
            // Make a station with one port and already occupy it
            var station = CreateStation(portCount: 1);
            station.ReserveDockingPort(_missionId, SupportedVehicle);

            var ex = Assert.Throws<AggregateValidationException>(
                () => station.ReserveDockingPort(_missionId, SupportedVehicle)
            );
            Assert.Contains("No available docking ports", ex.Message);
        }

        [Fact]
        public void ReleaseDockingPort_SucceedsAndFreesPort()
        {
            var station = CreateStation(portCount: 1);
            station.ReserveDockingPort(_missionId, SupportedVehicle);

            var portId = station.DockingPorts.Single().Id;
            station.ReleaseDockingPort(portId);

            var port = station.DockingPorts.Single();
            Assert.Equal(DockingPortStatus.Available, port.Status);
            Assert.Null(port.AssignedVehicle);
        }

        [Fact]
        public void ReleaseDockingPort_NotFound_Throws()
        {
            var station = CreateStation();
            var fakePortId = new DockingPortId(Guid.NewGuid());

            var ex = Assert.Throws<AggregateValidationException>(
                () => station.ReleaseDockingPort(fakePortId)
            );
            Assert.Contains("Docking port not found", ex.Message);
        }

        [Fact]
        public void AssignCrewMember_SucceedsAndActivates()
        {
            var station = CreateStation(maxCrew: 1);
            var crew = new LunarCrewMember(new LunarCrewMemberId(Guid.NewGuid()), "Alice", "Scientist");

            station.AssignCrewMember(crew);

            Assert.Single(station.CrewQuarters);
            var member = station.CrewQuarters.Single();
            Assert.Equal("Alice", member.Name);
            Assert.Equal(CrewAssignmentStatus.Active, member.AssignmentStatus);
        }

        [Fact]
        public void AssignCrewMember_OverCapacity_Throws()
        {
            var station = CreateStation(maxCrew: 1);
            station.AssignCrewMember(new LunarCrewMember(new LunarCrewMemberId(Guid.NewGuid()), "Alice", "Scientist"));

            var ex = Assert.Throws<AggregateValidationException>(
                () => station.AssignCrewMember(new LunarCrewMember(new LunarCrewMemberId(Guid.NewGuid()), "Bob", "Engineer"))
            );
            Assert.Contains("Station crew capacity exceeded", ex.Message);
        }

        [Fact]
        public void StorePayload_Succeeds()
        {
            var station = CreateStation(maxPayload: 50);
            var payload = new LunarPayload("Rover", 25, "ScienceLab");

            station.StorePayload(payload);

            Assert.Single(station.StoredPayloads);
            var stored = station.StoredPayloads.Single();
            Assert.Equal("Rover", stored.Description);
        }

        [Fact]
        public void StorePayload_OverCapacity_Throws()
        {
            var station = CreateStation(maxPayload: 50);
            station.StorePayload(new LunarPayload("Rover", 30, "ScienceLab"));

            var ex = Assert.Throws<AggregateValidationException>(
                () => station.StorePayload(new LunarPayload("Supplies", 25, "Storage"))
            );
            Assert.Contains("Payload capacity exceeded", ex.Message);
        }

        [Fact]
        public void RemoveCrewMember_Succeeds()
        {
            var station = CreateStation(maxCrew: 2);
            var crewId = new LunarCrewMemberId(Guid.NewGuid());
            var crew = new LunarCrewMember(crewId, "Alice", "Scientist");

            station.AssignCrewMember(crew);
            station.RemoveCrewMember(crewId);

            Assert.Empty(station.CrewQuarters);
        }

        [Fact]
        public void RemoveCrewMember_NotFound_Throws()
        {
            var station = CreateStation();
            var fakeId = new LunarCrewMemberId(Guid.NewGuid());

            var ex = Assert.Throws<AggregateValidationException>(
                () => station.RemoveCrewMember(fakeId)
            );
            Assert.Contains("Crew member not found", ex.Message);
        }

        [Fact]
        public void RemovePayload_Succeeds()
        {
            var station = CreateStation(maxPayload: 100);
            var payload = new LunarPayload("Rover", 25, "ScienceLab");

            station.StorePayload(payload);
            station.RemovePayload(payload);

            Assert.Empty(station.StoredPayloads);
        }

        [Fact]
        public void RemovePayload_NotFound_Throws()
        {
            var station = CreateStation();
            var payload = new LunarPayload("Rover", 25, "ScienceLab");

            var ex = Assert.Throws<AggregateValidationException>(
                () => station.RemovePayload(payload)
            );
            Assert.Contains("Matching payload not found", ex.Message);
        }
    }
}
