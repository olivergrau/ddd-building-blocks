// Domain/Aggregates/Mission.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Exception;
using JetBrains.Annotations;
using RocketLaunch.Domain.Model.Enums;
using RocketLaunch.SharedKernel.Events;
using RocketLaunch.SharedKernel.ValueObjects;
using AggregateException = DDD.BuildingBlocks.Core.Exception.AggregateException;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace RocketLaunch.Domain.Model
{
    public class Mission : AggregateRoot<MissionId>
    {
        // Public API
        public Mission(MissionId id,
                       MissionName name,
                       TargetOrbit target,
                       PayloadDescription payload,
                       LaunchWindow window)
            : base(id)
        {
            ArgumentNullException.ThrowIfNull(id);
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(payload);
            ArgumentNullException.ThrowIfNull(window);

            ApplyEvent(
                new MissionCreated(id, name, target, payload, window));
        }

        // For rehydration
        private Mission() : base(default!) { }

        public MissionName        Name            { get; private set; }
        public TargetOrbit        TargetOrbit     { get; private set; }
        public PayloadDescription Payload         { get; private set; }
        public LaunchWindow       Window          { get; private set; }
        public MissionStatus      Status          { get; private set; }
        public RocketId?          AssignedRocket  { get; private set; }
        public LaunchPadId?       AssignedPad     { get; private set; }
        public List<CrewMemberId> Crew            { get; } = new();

        // Commands
        public void AssignRocket(RocketId rocketId)
        {
            if (Status != MissionStatus.Planned)
                throw new AggregateValidationException(Id, nameof(Status), Status,"Can only assign rocket in Planned state");
            ApplyEvent(new RocketAssigned(Id, rocketId, CurrentVersion));
        }

        public void AssignLaunchPad(LaunchPadId padId)
        {
            if (AssignedRocket is null)
                throw new AggregateException(Id, "Rocket must be assigned first");
            ApplyEvent(new LaunchPadAssigned(Id, padId, CurrentVersion));
        }

        public void AssignCrew(IEnumerable<CrewMemberId> crew)
        {
            if (AssignedRocket is null || AssignedPad is null)
                throw new AggregateException(Id, "Rocket & pad must be assigned first");
            ApplyEvent(new CrewAssigned(Id, crew, CurrentVersion));
        }

        public void Schedule()
        {
            if (AssignedRocket is null || AssignedPad is null)
                throw new AggregateException(Id, "Resources incomplete");
            if (Status != MissionStatus.Planned)
                throw new AggregateException(Id, "Mission already scheduled or started");
            ApplyEvent(new MissionScheduled(Id, CurrentVersion));
        }

        public void Abort()
        {
            if (Status == MissionStatus.Launched)
                throw new AggregateException(Id, "Cannot abort after launch");
            ApplyEvent(new MissionAborted(Id, CurrentVersion));
        }

        public void MarkLaunched()
        {
            if (Status != MissionStatus.Scheduled)
                throw new AggregateException(Id, "Only scheduled missions can launch");
            ApplyEvent(new MissionLaunched(Id, CurrentVersion));
        }

        public void MarkArrived(DateTime arrivalTime,
                                string vehicleType,
                                IEnumerable<(string Name,string Role)> crewManifest,
                                IEnumerable<(string Item,double Mass)> payloadManifest)
        {
            if (Status != MissionStatus.Launched)
                throw new AggregateException(Id, "Only launched missions can arrive");
            ApplyEvent(new MissionArrivedAtLunarOrbit(
                Id, arrivalTime, vehicleType, crewManifest, payloadManifest, CurrentVersion));
        }

        // Event handlers
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(MissionCreated e)
        {
            Name        = e.Name;
            TargetOrbit = e.TargetOrbit;
            Payload     = e.Payload;
            Window      = e.LaunchWindow;
            Status      = MissionStatus.Planned;
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(RocketAssigned e)
        {
            AssignedRocket = e.RocketId;
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(LaunchPadAssigned e)
        {
            AssignedPad = e.PadId;
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(CrewAssigned e)
        {
            Crew.AddRange(e.Crew);
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(MissionScheduled e)
        {
            Status = MissionStatus.Scheduled;
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(MissionAborted e)
        {
            Status = MissionStatus.Aborted;
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(MissionLaunched e)
        {
            Status = MissionStatus.Launched;
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(MissionArrivedAtLunarOrbit e)
        {
            Status = MissionStatus.Arrived;
            // Integration-event will be published externally
        }

        protected override MissionId GetIdFromStringRepresentation(string id) => new(Guid.Parse(id));
    }
}
