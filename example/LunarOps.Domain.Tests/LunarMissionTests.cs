// LunarOps.Domain.Tests/LunarMissionTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using DDD.BuildingBlocks.Core;
using DDD.BuildingBlocks.Core.Exception;
using LunarOps.Domain.Model;
using LunarOps.SharedKernel.Enums;
using LunarOps.SharedKernel.Events;
using LunarOps.SharedKernel.ValueObjects;
using Xunit;

namespace LunarOps.Domain.Tests
{
    public class LunarMissionTests
    {
        private static LunarMission NewRegisteredMission(
            out ExternalMissionId missionId,
            out DateTime arrival,
            out VehicleType vehicle,
            out List<(string Name,string Role)> crewManifest,
            out List<(string Item,double Mass)> payloadManifest)
        {
            missionId = new ExternalMissionId(Guid.NewGuid().ToString());
            arrival = DateTime.UtcNow;
            vehicle = new VehicleType("LunarLander");
            crewManifest = new List<(string,string)> { ("Alice","Commander") };
            payloadManifest = new List<(string,double)> { ("Rover", 1500.0) };

            var mission = new LunarMission(
                missionId,
                arrival,
                vehicle,
                crewManifest,
                payloadManifest
            );
            // commit the registration event so tests start from Registered state
            mission.MarkChangesAsCommitted();
            return mission;
        }

        [Fact]
        public void Register_LunarMission_Should_Raise_LunarMissionRegistered_And_Be_Registered()
        {
            // arrange
            ExternalMissionId id = new ExternalMissionId("M-001");
            var arrival = new DateTime(2025, 7, 10, 12, 0, 0, DateTimeKind.Utc);
            var vehicle = new VehicleType("Orion");
            var crew = new List<(string,string)> { ("Bob","Pilot") };
            var payload = new List<(string,double)> { ("Probe", 100.0) };

            // act
            var mission = new LunarMission(id, arrival, vehicle, crew, payload);
            var events  = mission.GetUncommittedChanges().ToList();

            // assert
            Assert.Single(events);
            var e = Assert.IsType<LunarMissionRegistered>(events[0]);
            Assert.Equal(id, e.MissionId);
            Assert.Equal(arrival, e.ArrivalTime);
            Assert.Equal(vehicle, e.VehicleType);
            Assert.Equal(crew, e.CrewManifest);
            Assert.Equal(payload, e.PayloadManifest);
            Assert.Equal(LunarMissionStatus.Registered, mission.Status);
        }

        [Fact]
        public void ScheduleDocking_When_Registered_Should_Raise_DockingPortAssigned()
        {
            var mission = NewRegisteredMission(
                out var id, out _, out _, out _, out _);

            var stationId = new StationId(Guid.NewGuid());
            var portId    = new DockingPortId(Guid.NewGuid());

            mission.ScheduleDocking(stationId, portId);
            var events = mission.GetUncommittedChanges().ToList();

            Assert.Single(events);
            var e = Assert.IsType<DockingPortAssigned>(events[0]);
            Assert.Equal(id, e.MissionId);
            Assert.Equal(stationId, e.StationId);
            Assert.Equal(portId, e.PortId);
            Assert.Equal(LunarMissionStatus.DockingScheduled, mission.Status);
        }

        [Fact]
        public void CompleteDocking_Without_Scheduling_Should_Throw()
        {
            var mission = NewRegisteredMission(
                out _, out _, out _, out _, out _);

            Assert.Throws<AggregateValidationException>(() => mission.CompleteDocking());
        }

        [Fact]
        public void CompleteDocking_When_Scheduled_Should_Raise_LunarMissionDocked()
        {
            var mission = NewRegisteredMission(
                out var id, out _, out _, out _, out _);
            mission.ScheduleDocking(new StationId(Guid.NewGuid()), new DockingPortId(Guid.NewGuid()));
            mission.MarkChangesAsCommitted();

            mission.CompleteDocking();
            var events = mission.GetUncommittedChanges().ToList();

            Assert.Single(events);
            var e = Assert.IsType<LunarMissionDocked>(events[0]);
            Assert.Equal(id, e.MissionId);
            Assert.Equal(LunarMissionStatus.Docked, mission.Status);
        }

        [Fact]
        public void TransferCrew_Without_Docking_Should_Throw()
        {
            var mission = NewRegisteredMission(
                out _, out _, out _, out _, out _);

            var crewIds = new[] { new LunarCrewMemberId(Guid.NewGuid()) };
            Assert.Throws<AggregateValidationException>(() => mission.TransferCrew(crewIds));
        }

        [Fact]
        public void TransferCrew_When_Docked_Should_Raise_CrewTransferred()
        {
            var mission = NewRegisteredMission(
                out var id, out _, out _, out _, out _);
            mission.ScheduleDocking(new StationId(Guid.NewGuid()), new DockingPortId(Guid.NewGuid()));
            mission.CompleteDocking();
            mission.MarkChangesAsCommitted();

            var crewIds = new[] { new LunarCrewMemberId(Guid.NewGuid()), new LunarCrewMemberId(Guid.NewGuid()) };
            mission.TransferCrew(crewIds);
            var events = mission.GetUncommittedChanges().ToList();

            Assert.Single(events);
            var e = Assert.IsType<CrewTransferred>(events[0]);
            Assert.Equal(id.Value, e.MissionId);
            Assert.Equal(crewIds.Length, e.Crew.Count);
            Assert.Equal(crewIds, e.Crew);
            Assert.Equal(LunarMissionStatus.Unloaded, mission.Status);
        }

        [Fact]
        public void UnloadPayload_Without_Docking_Should_Throw()
        {
            var mission = NewRegisteredMission(
                out _, out _, out _, out _, out _);

            var payloadIds = new[] { new PayloadId(Guid.NewGuid()) };
            Assert.Throws<AggregateValidationException>(() => mission.UnloadPayload(payloadIds));
        }

        [Fact]
        public void UnloadPayload_When_Docked_Should_Raise_PayloadUnloaded()
        {
            var mission = NewRegisteredMission(
                out var id, out _, out _, out _, out _);
            mission.ScheduleDocking(new StationId(Guid.NewGuid()), new DockingPortId(Guid.NewGuid()));
            mission.CompleteDocking();
            mission.MarkChangesAsCommitted();

            var payloadIds = new[] { new PayloadId(Guid.NewGuid()), new PayloadId(Guid.NewGuid()) };
            mission.UnloadPayload(payloadIds);
            var events = mission.GetUncommittedChanges().ToList();

            Assert.Single(events);
            var e = Assert.IsType<PayloadUnloaded>(events[0]);
            Assert.Equal(id.Value, e.MissionId);
            Assert.Equal(payloadIds.Length, e.PayloadItems.Count);
            Assert.Equal(payloadIds, e.PayloadItems);
            Assert.Equal(LunarMissionStatus.Unloaded, mission.Status);
        }

        [Fact]
        public void MarkInService_Without_Unloaded_Should_Throw()
        {
            var mission = NewRegisteredMission(
                out _, out _, out _, out _, out _);

            Assert.Throws<AggregateValidationException>(() => mission.MarkInService());
        }

        [Fact]
        public void MarkInService_When_Unloaded_Should_Raise_LunarMissionInService()
        {
            var mission = NewRegisteredMission(
                out var id, out _, out _, out _, out _);
            mission.ScheduleDocking(new StationId(Guid.NewGuid()), new DockingPortId(Guid.NewGuid()));
            mission.CompleteDocking();
            mission.TransferCrew(new[] { new LunarCrewMemberId(Guid.NewGuid()) });
            mission.MarkChangesAsCommitted();

            mission.MarkInService();
            var events = mission.GetUncommittedChanges().ToList();

            Assert.Single(events);
            var e = Assert.IsType<LunarMissionInService>(events[0]);
            Assert.Equal(id, e.MissionId);
            Assert.Equal(LunarMissionStatus.InService, mission.Status);
        }

        [Fact]
        public void Depart_Without_InService_Should_Throw()
        {
            var mission = NewRegisteredMission(
                out _, out _, out _, out _, out _);

            Assert.Throws<AggregateValidationException>(() => mission.Depart());
        }

        [Fact]
        public void Depart_When_InService_Should_Raise_LunarMissionDeparted()
        {
            var mission = NewRegisteredMission(
                out var id, out _, out _, out _, out _);
            mission.ScheduleDocking(new StationId(Guid.NewGuid()), new DockingPortId(Guid.NewGuid()));
            mission.CompleteDocking();
            mission.TransferCrew(new[] { new LunarCrewMemberId(Guid.NewGuid()) });
            mission.MarkInService();
            mission.MarkChangesAsCommitted();

            mission.Depart();
            var events = mission.GetUncommittedChanges().ToList();

            Assert.Single(events);
            var e = Assert.IsType<LunarMissionDeparted>(events[0]);
            Assert.Equal(id, e.MissionId);
            Assert.Equal(LunarMissionStatus.Departed, mission.Status);
        }
    }
}
