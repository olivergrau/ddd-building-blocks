// RocketLaunch.Domain.Tests/MissionTests.cs

using DDD.BuildingBlocks.Core.Exception;
using RocketLaunch.Domain.Model;
using RocketLaunch.Domain.Model.Entities;
using RocketLaunch.Domain.Tests.Mocks;
using DDD.BuildingBlocks.Core.ErrorHandling;
using RocketLaunch.Domain.Service;
using RocketLaunch.ReadModel.Core.Exceptions;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.Events;
using RocketLaunch.SharedKernel.Events.Mission;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Domain.Tests
{
    public class MissionTests
    {
        private static Rocket NewRocket(out RocketId rocketId)
        {
            rocketId = new RocketId(Guid.NewGuid());
            return new Rocket(rocketId, "Falcon 9", 7600, 22800, 7);
        }
        
        private static LaunchPad NewLaunchPad(out LaunchPadId launchPadId)
        {
            launchPadId = new LaunchPadId(Guid.NewGuid());
            return new LaunchPad(launchPadId, "Pad 39A", "Cape Canaveral", ["Falcon 9", "Starship"]);
        }
        
        private static Mission NewPlannedMission(out MissionId missionId)
        {
            missionId = new MissionId(Guid.NewGuid());
            var name = new MissionName("Apollo");
            var orbit = new TargetOrbit("Lunar Orbit");
            var payload = new PayloadDescription("Rover + Supplies");
            var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(2));

            var mission = new Mission(missionId, name, orbit, payload, window);
            // commit the creation event so we can test next events in isolation
            mission.MarkChangesAsCommitted();
            return mission;
        }

        [Fact]
        public void Create_Mission_Should_Raise_MissionCreated_And_Be_Planned()
        {
            // arrange
            var id = new MissionId(Guid.NewGuid());
            var name = new MissionName("Test");
            var orbit = new TargetOrbit("LEO");
            var payload = new PayloadDescription("Sat");
            var window = new LaunchWindow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));

            // act
            var mission = new Mission(id, name, orbit, payload, window);
            var events = mission.GetUncommittedChanges().ToList();

            // assert
            Assert.Single(events);
            var created = Assert.IsType<MissionCreated>(events[0]);
            Assert.Equal(id, created.MissionId);
            Assert.Equal(name, created.Name);
            Assert.Equal(MissionStatus.Planned, mission.Status);
        }

        [Fact]
        public async Task AssignRocket_When_Planned_Should_Raise_RocketAssigned()
        {
            var mission = NewPlannedMission(out var id);
            var stub = new StubResourceAvailabilityService { RocketIsAvailable = true };
            
            var rocket = NewRocket(out var rocketId);
            
            await mission.AssignRocketAsync(rocket, stub);
            var events = mission.GetUncommittedChanges().ToList();

            Assert.Single(events);
            var evt = Assert.IsType<RocketAssigned>(events[0]);
            Assert.Equal(id, evt.MissionId);
            Assert.Equal(rocketId, evt.RocketId);
            Assert.Equal(rocketId, mission.AssignedRocket);
        }

        [Fact]
        public async Task AssignLaunchPad_Without_Rocket_Should_Throw()
        {
            var mission = NewPlannedMission(out _);

            var stub = new StubResourceAvailabilityService
            {
                RocketIsAvailable = true,
                LaunchPadIsAvailable = true
            };
            
            var launchPad = NewLaunchPad(out _);
            
            await Assert.ThrowsAsync<AggregateValidationException>(() =>
                mission.AssignLaunchPadAsync(launchPad, stub));
        }

        [Fact]
        public async Task AssignLaunchPad_With_Rocket_Should_Raise_LaunchPadAssigned()
        {
            var mission = NewPlannedMission(out var id);
            var rocket = NewRocket(out var rocketId);
            
            var stub = new StubResourceAvailabilityService
            {
                RocketIsAvailable = true,
                LaunchPadIsAvailable = true
            };

            await mission.AssignRocketAsync(rocket, stub);
            mission.MarkChangesAsCommitted();
            
            var launchPad = NewLaunchPad(out var padId);
            await mission.AssignLaunchPadAsync(launchPad, stub);
            var events = mission.GetUncommittedChanges().ToList();

            Assert.Single(events);
            var evt = Assert.IsType<LaunchPadAssigned>(events[0]);
            Assert.Equal(id, evt.MissionId);
            Assert.Equal(padId, evt.PadId);
            Assert.Equal(padId, mission.AssignedPad);
        }

        [Fact]
        public async Task AssignCrew_Without_RocketOrPad_Should_Throw()
        {
            var mission = NewPlannedMission(out _);
            var crew = new[] { new CrewMemberId(Guid.NewGuid()) };

            var stub = new StubResourceAvailabilityService
            {
                RocketIsAvailable = true,
                LaunchPadIsAvailable = true
            };

            await Assert.ThrowsAsync<AggregateValidationException>(() =>
                mission.AssignCrewAsync(crew, stub));
        }

        [Fact]
        public async Task AssignCrew_With_RocketAndPad_Should_Raise_CrewAssigned()
        {
            var mission = NewPlannedMission(out var id);

            var stub = new StubResourceAvailabilityService
            {
                RocketIsAvailable = true,
                LaunchPadIsAvailable = true,
                CrewIsAvailable = true
            };
            
            var rocket = NewRocket(out _);
   
            await mission.AssignRocketAsync(rocket, stub);
            var launchPad = NewLaunchPad(out _);
            await mission.AssignLaunchPadAsync(launchPad, stub);
            mission.MarkChangesAsCommitted();

            var crew = new[]
            {
                new CrewMemberId(Guid.NewGuid()),
                new CrewMemberId(Guid.NewGuid())
            };
            await mission.AssignCrewAsync(crew, stub);

            var events = mission.GetUncommittedChanges().ToList();
            Assert.Single(events);
            var evt = Assert.IsType<CrewAssigned>(events[0]);
            Assert.Equal(id, evt.MissionId);
            Assert.Equal(2, evt.Crew.Count);
            Assert.Equal(evt.Crew, crew);
            Assert.Equal(crew.Select(c => c.Value.ToString()), mission.Crew.Select(r => r.AggregateId));
        }

        [Fact]
        public void Schedule_Without_AllResources_Should_Throw()
        {
            var mission = NewPlannedMission(out _);
            // no rocket, pad or crew
            Assert.Throws<DDD.BuildingBlocks.Core.Exception.AggregateException>(() => mission.Schedule());
        }

        [Fact]
        public async Task Schedule_With_Resources_Should_Raise_MissionScheduled()
        {
            var stub = new StubResourceAvailabilityService
            {
                RocketIsAvailable = true,
                LaunchPadIsAvailable = true
            };
            
            var mission = NewPlannedMission(out var id);
            var rocket = NewRocket(out _);
            
            await mission.AssignRocketAsync(rocket, stub);
            var launchPad = NewLaunchPad(out _);
            await mission.AssignLaunchPadAsync(launchPad, stub);

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
        public async Task Abort_After_Launch_Should_Throw()
        {
            var stub = new StubResourceAvailabilityService
            {
                RocketIsAvailable = true,
                LaunchPadIsAvailable = true
            };

            var mission = NewPlannedMission(out _);
            var rocket = NewRocket(out _);
            
            await mission.AssignRocketAsync(rocket, stub);
            var launchPad = NewLaunchPad(out _);
            await mission.AssignLaunchPadAsync(launchPad, stub);
            mission.Schedule();
            mission.MarkChangesAsCommitted();
            mission.MarkLaunched();

            Assert.Throws<DDD.BuildingBlocks.Core.Exception.AggregateException>(() => mission.Abort());
        }

        [Fact]
        public async Task MarkLaunched_Only_When_Scheduled()
        {
            var stub = new StubResourceAvailabilityService
            {
                RocketIsAvailable = true,
                LaunchPadIsAvailable = true
            };

            var mission = NewPlannedMission(out _);
            Assert.Throws<DDD.BuildingBlocks.Core.Exception.AggregateException>(() => mission.MarkLaunched());

            var rocket = NewRocket(out _);
            
            await mission.AssignRocketAsync(rocket, stub);
            var launchPad = NewLaunchPad(out _);
            await mission.AssignLaunchPadAsync(launchPad, stub);
            mission.Schedule();
            mission.MarkChangesAsCommitted();

            mission.MarkLaunched();
            var events = mission.GetUncommittedChanges().ToList();
            Assert.Single(events);
            Assert.IsType<MissionLaunched>(events[0]);
            Assert.Equal(MissionStatus.Launched, mission.Status);
        }

        [Fact]
        public async Task MarkArrived_Only_When_Launched()
        {
            var stub = new StubResourceAvailabilityService
            {
                RocketIsAvailable = true,
                LaunchPadIsAvailable = true
            };

            var mission = NewPlannedMission(out _);
            Assert.Throws<DDD.BuildingBlocks.Core.Exception.AggregateException>(() =>
                mission.MarkArrived(DateTime.UtcNow, "TestVehicle",
                    new List<(string, string)>(), new List<(string, double)>()));

            var rocket = NewRocket(out _);
            
            await mission.AssignRocketAsync(rocket, stub);
            var launchPad = NewLaunchPad(out _);
            await mission.AssignLaunchPadAsync(launchPad, stub);
            mission.Schedule();
            mission.MarkLaunched();
            mission.MarkChangesAsCommitted();

            var arrivalTime = DateTime.UtcNow;
            var crew = new[] { ("Alice", "Commander") };
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


        [Fact]
        public async Task AssignRocket_When_Rocket_Is_Not_Available_Should_Throw()
        {
            var mission = NewPlannedMission(out _);
            var rocketId = new RocketId(Guid.NewGuid());

            var stub = new StubResourceAvailabilityService
            {
                RocketIsAvailable = false // simulate unavailability
            };
            
            var rocket = NewRocket(out _);
            
            var ex = await Assert.ThrowsAsync<RuleValidationException>(() => mission.AssignRocketAsync(rocket, stub));

            Assert.Contains("Rocket not available", ex.Message);
            Assert.Empty(mission.GetUncommittedChanges());
        }

        [Fact]
        public async Task AssignLaunchPad_When_Not_Available_Should_Throw()
        {
            var mission = NewPlannedMission(out _);
            var rocketId = new RocketId(Guid.NewGuid());

            var stub = new StubResourceAvailabilityService
            {
                RocketIsAvailable = true,
                LaunchPadIsAvailable = false
            };

            var rocket = NewRocket(out _);
            
            await mission.AssignRocketAsync(rocket, stub);
            mission.MarkChangesAsCommitted();

            var padId = new LaunchPadId(Guid.NewGuid());
            
            var launchPad = NewLaunchPad(out _);
            
            var ex = await Assert.ThrowsAsync<RuleValidationException>(() => mission.AssignLaunchPadAsync(launchPad, stub));

            Assert.Contains("LaunchPad not available", ex.Message);
            Assert.Empty(mission.GetUncommittedChanges());
        }

        [Fact]
        public async Task AssignCrew_When_Crew_Not_Available_Should_Throw()
        {
            var mission = NewPlannedMission(out _);

            var stub = new StubResourceAvailabilityService
            {
                RocketIsAvailable = true,
                LaunchPadIsAvailable = true,
                CrewIsAvailable = false
            };

            var rocket = NewRocket(out _);
            
            await mission.AssignRocketAsync(rocket, stub);
            var launchPad = NewLaunchPad(out _);
            await mission.AssignLaunchPadAsync(launchPad, stub);
            mission.MarkChangesAsCommitted();

            var crew = new[]
            {
                new CrewMemberId(Guid.NewGuid()),
                new CrewMemberId(Guid.NewGuid())
            };

            var ex = await Assert.ThrowsAsync<RuleValidationException>(() => mission.AssignCrewAsync(crew, stub));

            Assert.Contains("Crew not available", ex.Message);
            Assert.Empty(mission.GetUncommittedChanges());
            Assert.Empty(mission.Crew);
        }

        [Fact]
        public async Task AssignRocket_When_Service_Fails_Should_Bubble_Up()
        {
            var mission = NewPlannedMission(out _);
            var rocket = NewRocket(out _);
            var failing = new FailingAvailabilityService(FailMode.Rocket);

            await Assert.ThrowsAsync<ReadModelServiceException>(() => mission.AssignRocketAsync(rocket, failing));
        }

        [Fact]
        public async Task AssignLaunchPad_When_Service_Fails_Should_Bubble_Up()
        {
            var mission = NewPlannedMission(out _);
            var rocket = NewRocket(out _);
            await mission.AssignRocketAsync(rocket, new StubResourceAvailabilityService());
            mission.MarkChangesAsCommitted();

            var pad = NewLaunchPad(out _);
            var failing = new FailingAvailabilityService(FailMode.Pad);

            await Assert.ThrowsAsync<ReadModelServiceException>(() => mission.AssignLaunchPadAsync(pad, failing));
        }

        [Fact]
        public async Task AssignCrew_When_Service_Fails_Should_Bubble_Up()
        {
            var mission = NewPlannedMission(out _);
            var rocket = NewRocket(out _);
            await mission.AssignRocketAsync(rocket, new StubResourceAvailabilityService());
            var pad = NewLaunchPad(out _);
            await mission.AssignLaunchPadAsync(pad, new StubResourceAvailabilityService());
            mission.MarkChangesAsCommitted();

            var crew = new[] { new CrewMemberId(Guid.NewGuid()) };
            var failing = new FailingAvailabilityService(FailMode.Crew);

            await Assert.ThrowsAsync<ReadModelServiceException>(() => mission.AssignCrewAsync(crew, failing));
        }

        private enum FailMode { Rocket, Pad, Crew }

        private class FailingAvailabilityService : IResourceAvailabilityService
        {
            private readonly FailMode _mode;

            public FailingAvailabilityService(FailMode mode) => _mode = mode;

            public Task<bool> IsRocketAvailableAsync(RocketId rocketId, LaunchWindow window)
            {
                if (_mode == FailMode.Rocket)
                    throw new ReadModelServiceException("fail", ErrorClassification.TransientFailure);
                return Task.FromResult(true);
            }

            public Task<bool> IsLaunchPadAvailableAsync(LaunchPadId padId, LaunchWindow window)
            {
                if (_mode == FailMode.Pad)
                    throw new ReadModelServiceException("fail", ErrorClassification.TransientFailure);
                return Task.FromResult(true);
            }

            public Task<bool> AreCrewMembersAvailableAsync(IEnumerable<CrewMemberId> crewIds, LaunchWindow window)
            {
                if (_mode == FailMode.Crew)
                    throw new ReadModelServiceException("fail", ErrorClassification.TransientFailure);
                return Task.FromResult(true);
            }
        }
    }
}
