// Tests/Domain/Model/LunarMissionTests.cs

using System.Reflection;
using DDD.BuildingBlocks.Core.Exception;
using LunarOps.Domain.Model;
using LunarOps.Domain.Service;
using LunarOps.SharedKernel.Enums;
using LunarOps.SharedKernel.ValueObjects;
using Xunit;

namespace LunarOps.Domain.Tests
{
    public class LunarMissionTests
    {
        // Dummy IStationAvailabilityService for testing
        private class TestStationAvailabilityService : IStationAvailabilityService
        {
            private readonly bool _crewOk;
            private readonly bool _storageOk;
            private readonly bool _supportOk;

            public TestStationAvailabilityService(bool crewOk = true, bool storageOk = true, bool supportOk = true)
            {
                _crewOk = crewOk;
                _storageOk = storageOk;
                _supportOk = supportOk;
            }

            public Task<bool> HasCrewCapacityAsync(StationId stationId, int crewCount)
                => Task.FromResult(_crewOk);

            public Task<bool> HasStorageCapacityAsync(StationId stationId, double payloadMass)
                => Task.FromResult(_storageOk);

            public Task<bool> HasSupportedVehicleTypeAsync(StationId stationId, VehicleType vehicleType)
                => Task.FromResult(_supportOk);

            public Task<bool> HasFreePortAsync(StationId stationId)
                => Task.FromResult(true);
        }

        private readonly ExternalMissionId _missionId = new(Guid.NewGuid().ToString());
        private readonly StationId _stationId = new(Guid.NewGuid());
        private readonly DateTime _arrivalTime = DateTime.UtcNow;
        private readonly VehicleType _vehicleType = new("Starship");
        private readonly List<(string Name, string Role)> _crewManifest =
            [("Alice", "Commander"), ("Bob", "Scientist")];
        private readonly List<(string Item, double Mass)> _payloadManifest =
            [("Rover", 10.0), ("Supplies", 20.0)];

        [Fact]
        public void Constructor_WithValidParams_SetsRegisteredAndRelation()
        {
            var validator = new TestStationAvailabilityService();
            var mission = new LunarMission(
                _missionId,
                _arrivalTime,
                _vehicleType,
                _crewManifest,
                _payloadManifest,
                _stationId,
                validator);

            Assert.Equal(_arrivalTime, mission.ArrivalTime);
            Assert.Equal(_vehicleType, mission.VehicleType);
            Assert.Equal(_crewManifest, mission.CrewManifest);
            Assert.Equal(_payloadManifest, mission.PayloadManifest);
            Assert.Equal(LunarMissionStatus.Registered, mission.Status);
            Assert.Equal(_stationId.Value.ToString(), mission.StationRelation.AggregateId);
        }

        [Fact]
        public void Constructor_InsufficientCrewCapacity_ThrowsRuleValidationException()
        {
            var validator = new TestStationAvailabilityService(crewOk: false);
            Assert.Throws<RuleValidationException>(() =>
                new LunarMission(
                    _missionId,
                    _arrivalTime,
                    _vehicleType,
                    _crewManifest,
                    _payloadManifest,
                    _stationId,
                    validator));
        }

        [Fact]
        public void Constructor_InsufficientStorageCapacity_ThrowsRuleValidationException()
        {
            var validator = new TestStationAvailabilityService(storageOk: false);
            Assert.Throws<RuleValidationException>(() =>
                new LunarMission(
                    _missionId,
                    _arrivalTime,
                    _vehicleType,
                    _crewManifest,
                    _payloadManifest,
                    _stationId,
                    validator));
        }

        [Fact]
        public void Constructor_UnsupportedVehicleType_ThrowsRuleValidationException()
        {
            var validator = new TestStationAvailabilityService(supportOk: false);
            Assert.Throws<RuleValidationException>(() =>
                new LunarMission(
                    _missionId,
                    _arrivalTime,
                    _vehicleType,
                    _crewManifest,
                    _payloadManifest,
                    _stationId,
                    validator));
        }

        [Fact]
        public void AssignDockingPort_Valid_TransitionsToDockingScheduled()
        {
            var validator = new TestStationAvailabilityService();
            var mission = new LunarMission(
                _missionId,
                _arrivalTime,
                _vehicleType,
                _crewManifest,
                _payloadManifest,
                _stationId,
                validator);

            var somePortId = new DockingPortId(Guid.NewGuid());
            mission.AssignDockingPort(somePortId);

            Assert.Equal(LunarMissionStatus.DockingScheduled, mission.Status);
            Assert.Equal(somePortId, mission.AssignedPort);
        }

        [Theory]
        [InlineData(LunarMissionStatus.DockingScheduled)]
        [InlineData(LunarMissionStatus.Docked)]
        [InlineData(LunarMissionStatus.InService)]
        public void AssignDockingPort_InvalidState_Throws(LunarMissionStatus status)
        {
            var validator = new TestStationAvailabilityService();
            var mission = new LunarMission(
                _missionId,
                _arrivalTime,
                _vehicleType,
                _crewManifest,
                _payloadManifest,
                _stationId,
                validator);

            // Find the public property (even though its setter is private):
            var statusProp = typeof(LunarMission)
                .GetProperty(
                    "Status",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

            if (statusProp == null)
                throw new InvalidOperationException("Could not find 'Status' property via reflection.");

            // Now set the private setter:
            statusProp.SetValue(mission, status);

            Assert.Throws<AggregateValidationException>(
                () => mission.AssignDockingPort(new DockingPortId(Guid.NewGuid()))
            );
        }

        [Fact]
        public void CompleteDocking_Valid_TransitionsToDocked()
        {
            var validator = new TestStationAvailabilityService();
            var mission = new LunarMission(
                _missionId,
                _arrivalTime,
                _vehicleType,
                _crewManifest,
                _payloadManifest,
                _stationId,
                validator);

            var portId = new DockingPortId(Guid.NewGuid());
            mission.AssignDockingPort(portId);
            mission.CompleteDocking();

            Assert.Equal(LunarMissionStatus.Docked, mission.Status);
        }

        [Fact]
        public void CompleteDocking_InvalidState_Throws()
        {
            var validator = new TestStationAvailabilityService();
            var mission = new LunarMission(
                _missionId,
                _arrivalTime,
                _vehicleType,
                _crewManifest,
                _payloadManifest,
                _stationId,
                validator);

            Assert.Throws<AggregateValidationException>(() => mission.CompleteDocking());
        }

        [Fact]
        public void UnloadPayload_ValidAfterDocked_TransitionsToPayloadUnloaded()
        {
            var validator = new TestStationAvailabilityService();
            var mission = new LunarMission(
                _missionId,
                _arrivalTime,
                _vehicleType,
                _crewManifest,
                _payloadManifest,
                _stationId,
                validator);

            mission.AssignDockingPort(new DockingPortId(Guid.NewGuid()));
            mission.CompleteDocking();
            mission.UnloadPayload();

            Assert.Equal(LunarMissionStatus.PayloadUnloaded, mission.Status);
        }

        [Fact]
        public void UnloadPayload_InvalidState_Throws()
        {
            var validator = new TestStationAvailabilityService();
            var mission = new LunarMission(
                _missionId,
                _arrivalTime,
                _vehicleType,
                _crewManifest,
                _payloadManifest,
                _stationId,
                validator);

            Assert.Throws<AggregateValidationException>(() => mission.UnloadPayload());
        }

        [Fact]
        public void TransferCrew_ValidAfterDocked_TransitionsToCrewTransferred()
        {
            var validator = new TestStationAvailabilityService();
            var mission = new LunarMission(
                _missionId,
                _arrivalTime,
                _vehicleType,
                _crewManifest,
                _payloadManifest,
                _stationId,
                validator);

            mission.AssignDockingPort(new DockingPortId(Guid.NewGuid()));
            mission.CompleteDocking();
            mission.TransferCrew(_crewManifest.Select((c, _) => new LunarCrewMemberId(Guid.NewGuid())));
            
            Assert.Equal(LunarMissionStatus.CrewTransferred, mission.Status);
        }

        [Fact]
        public void TransferCrew_InvalidState_Throws()
        {
            var validator = new TestStationAvailabilityService();
            var mission = new LunarMission(
                _missionId,
                _arrivalTime,
                _vehicleType,
                _crewManifest,
                _payloadManifest,
                _stationId,
                validator);

            Assert.Throws<AggregateValidationException>(() =>
                mission.TransferCrew(_crewManifest.Select((c, _) => new LunarCrewMemberId(Guid.NewGuid()))));
        }

        [Fact]
        public void MarkInService_InvalidState_Throws()
        {
            var validator = new TestStationAvailabilityService();
            var mission = new LunarMission(
                _missionId,
                _arrivalTime,
                _vehicleType,
                _crewManifest,
                _payloadManifest,
                _stationId,
                validator);

            Assert.Throws<AggregateValidationException>(() => mission.MarkInService());
        }

        [Fact]
        public void Depart_InvalidState_Throws()
        {
            var validator = new TestStationAvailabilityService();
            var mission = new LunarMission(
                _missionId,
                _arrivalTime,
                _vehicleType,
                _crewManifest,
                _payloadManifest,
                _stationId,
                validator);

            Assert.Throws<AggregateValidationException>(() => mission.Depart());
        }
    }
}
