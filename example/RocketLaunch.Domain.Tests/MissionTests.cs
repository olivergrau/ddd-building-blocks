// RocketLaunch.Domain.Tests/MissionTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using DDD.BuildingBlocks.Core;
using RocketLaunch.Domain.Model;
using RocketLaunch.Domain.Model.Enums;
using RocketLaunch.SharedKernel.Events;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Domain.Tests
{
    public class MissionTests
    {
        private static Mission NewPlannedMission(out MissionId missionId)
        {
            missionId = new MissionId(Guid.NewGuid());
            var name     = new MissionName("Apollo");
            var orbit    = new TargetOrbit("Lunar Orbit");
            var payload  = new PayloadDescription("Rover + Supplies");
            var window   = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(2));

            var mission = new Mission(missionId, name, orbit, payload, window);
            // commit the creation event so we can test next events in isolation
            mission.MarkChangesAsCommitted();
            return mission;
        }

        [Fact]
        public void Create_Mission_Should_Raise_MissionCreated_And_Be_Planned()
        {
            // arrange
            var id     = new MissionId(Guid.NewGuid());
            var name   = new MissionName("Test");
            var orbit  = new TargetOrbit("LEO");
            var payload= new PayloadDescription("Sat");
            var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));

            // act
            var mission = new Mission(id, name, orbit, payload, window);
            var events  = mission.GetUncommittedChanges().ToList();

            // assert
            Assert.Single(events);
            var created = Assert.IsType<MissionCreated>(events[0]);
            Assert.Equal(id, created.MissionId);
            Assert.Equal(name, created.Name);
            Assert.Equal(MissionStatus.Planned, mission.Status);
        }

        [Fact]
        public void AssignRocket_When_Planned_Should_Raise_RocketAssigned()
        {
            var mission = NewPlannedMission(out var id);
            var rocketId = new RocketId(Guid.NewGuid());

            mission.AssignRocket(rocketId);
            var events = mission.GetUncommittedChanges().ToList();

            Assert.Single(events);
            var evt = Assert.IsType<RocketAssigned>(events[0]);
            Assert.Equal(id, evt.MissionId);
            Assert.Equal(rocketId, evt.RocketId);
            Assert.Equal(rocketId, mission.AssignedRocket);
        }

        [Fact]
        public void AssignLaunchPad_Without_Rocket_Should_Throw()
        {
            var mission = NewPlannedMission(out _);

            Assert.Throws<DDD.BuildingBlocks.Core.Exception.AggregateException>(
                () => mission.AssignLaunchPad(new LaunchPadId(Guid.NewGuid())));
        }

        [Fact]
        public void AssignLaunchPad_With_Rocket_Should_Raise_LaunchPadAssigned()
        {
            var mission = NewPlannedMission(out var id);
            var rocketId = new RocketId(Guid.NewGuid());
            mission.AssignRocket(rocketId);
            mission.MarkChangesAsCommitted();

            var padId = new LaunchPadId(Guid.NewGuid());
            mission.AssignLaunchPad(padId);
            var events = mission.GetUncommittedChanges().ToList();

            Assert.Single(events);
            var evt = Assert.IsType<LaunchPadAssigned>(events[0]);
            Assert.Equal(id, evt.MissionId);
            Assert.Equal(padId, evt.PadId);
            Assert.Equal(padId, mission.AssignedPad);
        }

        [Fact]
        public void AssignCrew_Without_RocketOrPad_Should_Throw()
        {
            var mission = NewPlannedMission(out _);
            var crew = new[] { new CrewMemberId(Guid.NewGuid()) };

            Assert.Throws<DDD.BuildingBlocks.Core.Exception.AggregateException>(() => mission.AssignCrew(crew));
        }

        [Fact]
        public void AssignCrew_With_RocketAndPad_Should_Raise_CrewAssigned()
        {
            var mission = NewPlannedMission(out var id);
            mission.AssignRocket(new RocketId(Guid.NewGuid()));
            mission.AssignLaunchPad(new LaunchPadId(Guid.NewGuid()));
            mission.MarkChangesAsCommitted();

            var crew = new[] {
                new CrewMemberId(Guid.NewGuid()),
                new CrewMemberId(Guid.NewGuid())
            };
            mission.AssignCrew(crew);

            var events = mission.GetUncommittedChanges().ToList();
            Assert.Single(events);
            var evt = Assert.IsType<CrewAssigned>(events[0]);
            Assert.Equal(id, evt.MissionId);
            Assert.Equal(2, evt.Crew.Count);
            Assert.Equal(evt.Crew, crew);
        }

        [Fact]
        public void Schedule_Without_AllResources_Should_Throw()
        {
            var mission = NewPlannedMission(out _);
            // no rocket, pad or crew
            Assert.Throws<DDD.BuildingBlocks.Core.Exception.AggregateException>(() => mission.Schedule());
        }

        [Fact]
        public void Schedule_With_Resources_Should_Raise_MissionScheduled()
        {
            var mission = NewPlannedMission(out var id);
            mission.AssignRocket(new RocketId(Guid.NewGuid()));
            mission.AssignLaunchPad(new LaunchPadId(Guid.NewGuid()));
            mission.MarkChangesAsCommitted();

            mission.Schedule();
            var events = mission.GetUncommittedChanges().ToList();

            Assert.Single(events);
            var evt = Assert.IsType<MissionScheduled>(events[0]);
            Assert.Equal(id, evt.MissionId);
            Assert.Equal(MissionStatus.Scheduled, mission.Status);
        }

        [Fact]
        public void Abort_Before_Launch_Should_Raise_MissionAborted()
        {
            var mission = NewPlannedMission(out var id);

            mission.Abort();
            var events = mission.GetUncommittedChanges().ToList();

            Assert.Single(events);
            var evt = Assert.IsType<MissionAborted>(events[0]);
            Assert.Equal(id, evt.MissionId);
            Assert.Equal(MissionStatus.Aborted, mission.Status);
        }

        [Fact]
        public void Abort_After_Launch_Should_Throw()
        {
            var mission = NewPlannedMission(out _);
            mission.AssignRocket(new RocketId(Guid.NewGuid()));
            mission.AssignLaunchPad(new LaunchPadId(Guid.NewGuid()));
            mission.Schedule();
            mission.MarkChangesAsCommitted();
            mission.MarkLaunched();

            Assert.Throws<DDD.BuildingBlocks.Core.Exception.AggregateException>(() => mission.Abort());
        }

        [Fact]
        public void MarkLaunched_Only_When_Scheduled()
        {
            var mission = NewPlannedMission(out _);
            Assert.Throws<DDD.BuildingBlocks.Core.Exception.AggregateException>(() => mission.MarkLaunched());

            mission.AssignRocket(new RocketId(Guid.NewGuid()));
            mission.AssignLaunchPad(new LaunchPadId(Guid.NewGuid()));
            mission.Schedule();
            mission.MarkChangesAsCommitted();

            mission.MarkLaunched();
            var events = mission.GetUncommittedChanges().ToList();
            Assert.Single(events);
            Assert.IsType<MissionLaunched>(events[0]);
            Assert.Equal(MissionStatus.Launched, mission.Status);
        }

        [Fact]
        public void MarkArrived_Only_When_Launched()
        {
            var mission = NewPlannedMission(out _);
            Assert.Throws<DDD.BuildingBlocks.Core.Exception.AggregateException>(() =>
                mission.MarkArrived(DateTime.UtcNow, "TestVehicle", 
                    new List<(string, string)>(), new List<(string, double)>()));

            mission.AssignRocket(new RocketId(Guid.NewGuid()));
            mission.AssignLaunchPad(new LaunchPadId(Guid.NewGuid()));
            mission.Schedule();
            mission.MarkLaunched();
            mission.MarkChangesAsCommitted();

            var arrivalTime = DateTime.UtcNow;
            var crew = new[] { ("Alice","Commander") };
            var payload = new[] { ("Probe", 100.0) };

            mission.MarkArrived(arrivalTime, "Starship", crew, payload);
            var events = mission.GetUncommittedChanges().ToList();

            Assert.Single(events);
            var evt = Assert.IsType<MissionArrivedAtLunarOrbit>(events[0]);
            Assert.Equal(arrivalTime, evt.ArrivalTime);
            Assert.Equal("Starship", evt.VehicleType);
            Assert.Equal(crew, evt.CrewManifest);
            Assert.Equal(payload, evt.PayloadManifest);
            Assert.Equal(MissionStatus.Arrived, mission.Status);
        }
    }
}
