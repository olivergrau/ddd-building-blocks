using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Exception;
using JetBrains.Annotations;
using LunarOps.Domain.Service;
using LunarOps.SharedKernel.Enums;
using LunarOps.SharedKernel.Events.LunarMission;
using LunarOps.SharedKernel.ValueObjects;
// ReSharper disable PossibleMultipleEnumeration

namespace LunarOps.Domain.Model
{
    public class LunarMission : AggregateRoot<ExternalMissionId>
    {
        public DateTime ArrivalTime { get; private set; }
        public VehicleType VehicleType { get; private set; }
        public IReadOnlyCollection<(string Name, string Role)> CrewManifest { get; private set; }
        public IReadOnlyCollection<(string Item, double Mass)> PayloadManifest { get; private set; }
        public LunarMissionStatus Status { get; private set; }

        public DomainRelation StationRelation { get; private set; }
        public DockingPortId? AssignedPort { get; private set; }

        public LunarMission(
            ExternalMissionId missionId,
            DateTime arrivalTime,
            VehicleType vehicleType,
            IEnumerable<(string Name, string Role)> crewManifest,
            IEnumerable<(string Item, double Mass)> payloadManifest,
            StationId assignedStationId,
            IStationAvailabilityService validator)
            : base(missionId)
        {
            ArgumentNullException.ThrowIfNull(validator);

            var totalPayloadMass = payloadManifest.Sum(p => p.Mass);
            var crewCount = crewManifest.Count();

            // Validate station suitability
            if (!validator.HasCrewCapacityAsync(assignedStationId, crewCount).GetAwaiter().GetResult())
                throw new RuleValidationException(missionId, "Station has insufficient crew capacity");

            if (!validator.HasStorageCapacityAsync(assignedStationId, totalPayloadMass).GetAwaiter().GetResult())
                throw new RuleValidationException(missionId, "Station has insufficient payload storage capacity");

            if (!validator.HasSupportedVehicleTypeAsync(assignedStationId, vehicleType).GetAwaiter().GetResult())
                throw new RuleValidationException(missionId, "Station has no support for the vehicle type");

            ApplyEvent(new LunarMissionRegistered(
                missionId,
                arrivalTime,
                vehicleType,
                crewManifest,
                payloadManifest,
                assignedStationId
            ));
        }

        [UsedImplicitly]
        private LunarMission() : base(default!) { }
        
        // Business Methods
        public void AssignDockingPort(DockingPortId portId)
        {
            if (Status == LunarMissionStatus.Departed)
                throw new AggregateValidationException(Id, nameof(Status), Status, "Mission has already departed and cannot assign a docking port.");
            
            if (Status != LunarMissionStatus.Registered)
                throw new AggregateValidationException(Id, nameof(Status), Status, "Only registered missions can assign a docking port.");

            ApplyEvent(new DockingPortAssigned(Id, portId, CurrentVersion));
        }

        public void CompleteDocking()
        {
            if (Status == LunarMissionStatus.Departed)
                throw new AggregateValidationException(Id, nameof(Status), Status, "Mission has already departed and cannot assign a docking port.");

            if (Status != LunarMissionStatus.DockingScheduled)
                throw new AggregateValidationException(Id, nameof(Status), Status, "Only docking-scheduled missions can complete docking.");

            ApplyEvent(new LunarMissionDocked(Id, CurrentVersion));
        }
        
        public void UnloadPayload()
        {
            if (Status == LunarMissionStatus.Departed)
                throw new AggregateValidationException(Id, nameof(Status), Status, "Mission has already departed and cannot assign a docking port.");

            if (Status != LunarMissionStatus.Docked && Status != LunarMissionStatus.CrewTransferred)
                throw new AggregateValidationException(Id, nameof(Status), Status, "Mission must be docked before unloading payload.");

            var payloads = PayloadManifest.Select(p => new LunarPayload(p.Item, p.Mass, "Unknown")).ToList(); // fix if DestinationArea exists
            ApplyEvent(new PayloadUnloaded(Id, payloads, CurrentVersion));
        }
        
        public void TransferCrew(IEnumerable<LunarCrewMemberId> crewIds)
        {
            if (Status == LunarMissionStatus.Departed)
                throw new AggregateValidationException(Id, nameof(Status), Status, "Mission has already departed and cannot assign a docking port.");

            if (Status != LunarMissionStatus.Docked && Status != LunarMissionStatus.PayloadUnloaded)
                throw new AggregateValidationException(Id, nameof(Status), Status, "Mission must be docked to transfer crew.");

            ApplyEvent(new CrewTransferred(Id, crewIds, CurrentVersion));
        }
        
        public void MarkInService()
        {
            if (Status == LunarMissionStatus.Departed)
                throw new AggregateValidationException(Id, nameof(Status), Status, "Mission has already departed and cannot assign a docking port.");

            if (Status != LunarMissionStatus.ReadyForService)
                throw new AggregateValidationException(Id, nameof(Status), Status, "Mission must have transferred crew and unloaded payload before marking as in-service.");

            ApplyEvent(new LunarMissionInService(Id, CurrentVersion));
        }
        
        public void Depart()
        {
            if (Status != LunarMissionStatus.InService)
                throw new AggregateValidationException(Id, nameof(Status), Status, "Mission is not in service and cannot depart.");

            ApplyEvent(new LunarMissionDeparted(Id, CurrentVersion));
        }

        // Event Handlers
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(LunarMissionRegistered e)
        {
            ArrivalTime = e.ArrivalTime;
            VehicleType = e.VehicleType;
            CrewManifest = new List<(string, string)>(e.CrewManifest).AsReadOnly();
            PayloadManifest = new List<(string, double)>(e.PayloadManifest).AsReadOnly();
            Status = LunarMissionStatus.Registered;
            StationRelation = new DomainRelation(e.AssignedStationId.Value.ToString());
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(DockingPortAssigned e)
        {
            AssignedPort = e.PortId;
            Status = LunarMissionStatus.DockingScheduled;
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(LunarMissionDocked e)
        {
            Status = LunarMissionStatus.Docked;
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(PayloadUnloaded e)
        {
            if (Status == LunarMissionStatus.CrewTransferred)
            {
                Status = LunarMissionStatus.ReadyForService;
                return;
            }

            Status = LunarMissionStatus.PayloadUnloaded;
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(CrewTransferred e)
        {
            if (Status == LunarMissionStatus.PayloadUnloaded)
            {
                Status = LunarMissionStatus.ReadyForService;
                return;
            }

            Status = LunarMissionStatus.CrewTransferred;
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(LunarMissionInService e)
        {
            Status = LunarMissionStatus.InService;
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(LunarMissionDeparted e)
        {
            Status = LunarMissionStatus.Departed;
        }

        protected override ExternalMissionId GetIdFromStringRepresentation(string id) => new(id);
    }
}
