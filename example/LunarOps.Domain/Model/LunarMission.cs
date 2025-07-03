// Domain/Aggregates/LunarMission.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Exception;
using JetBrains.Annotations;
using LunarOps.SharedKernel.Enums;
using LunarOps.SharedKernel.Events;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.Domain.Model
{
    public class LunarMission : AggregateRoot<ExternalMissionId>
    {
        // State
        public DateTime                       ArrivalTime      { get; private set; }
        public VehicleType                    VehicleType      { get; private set; }
        public IReadOnlyCollection<(string Name,string Role)> CrewManifest { get; private set; }
        public IReadOnlyCollection<(string Item,double Mass)> PayloadManifest { get; private set; }
        public LunarMissionStatus             Status           { get; private set; }
        public StationId?                     AssignedStation  { get; private set; }
        public DockingPortId?                 AssignedPort     { get; private set; }

        // Constructor for registration (triggered by integration event)
        public LunarMission(
            ExternalMissionId missionId,
            DateTime arrivalTime,
            VehicleType vehicleType,
            IEnumerable<(string Name,string Role)> crewManifest,
            IEnumerable<(string Item,double Mass)> payloadManifest
        ) : base(missionId)
        {
            ArgumentNullException.ThrowIfNull(missionId);
            ArgumentNullException.ThrowIfNull(vehicleType);
            ArgumentNullException.ThrowIfNull(crewManifest);
            ArgumentNullException.ThrowIfNull(payloadManifest);
            
            ApplyEvent(new LunarMissionRegistered(
                missionId,
                arrivalTime,
                vehicleType,
                crewManifest,
                payloadManifest
            ));
        }

        // For rehydration
        private LunarMission() : base(default!) { }

        // Behaviors
        public void ScheduleDocking(StationId station, DockingPortId port)
        {
            if (Status != LunarMissionStatus.Registered)
                throw new AggregateValidationException(Id, nameof(Status), Status, "Only Registered missions can schedule docking");
            ApplyEvent(new DockingPortAssigned(Id, station, port, CurrentVersion));
        }

        public void CompleteDocking()
        {
            if (Status != LunarMissionStatus.DockingScheduled)
                throw new AggregateValidationException(Id, nameof(Status), Status,"Docking not scheduled");
            ApplyEvent(new LunarMissionDocked(Id, CurrentVersion));
        }

        public void TransferCrew(IEnumerable<LunarCrewMemberId> crewIds)
        {
            if (Status != LunarMissionStatus.Docked)
                throw new AggregateValidationException(Id, nameof(Status), Status,"Mission not docked");
            ApplyEvent(new CrewTransferred(Id, crewIds, CurrentVersion));
        }

        public void UnloadPayload(IEnumerable<PayloadId> payloadIds)
        {
            if (Status != LunarMissionStatus.Docked)
                throw new AggregateValidationException(Id, nameof(Status), Status,"Mission not docked");
            ApplyEvent(new PayloadUnloaded(Id, payloadIds, CurrentVersion));
        }

        public void MarkInService()
        {
            if (Status != LunarMissionStatus.Unloaded)
                throw new AggregateValidationException(Id, nameof(Status), Status,"Must unload before in-service");
            ApplyEvent(new LunarMissionInService(Id, CurrentVersion));
        }

        public void Depart()
        {
            if (Status != LunarMissionStatus.InService)
                throw new AggregateValidationException(Id, nameof(Status), Status,"Mission not in service");
            ApplyEvent(new LunarMissionDeparted(Id, CurrentVersion));
        }

        // Event handlers
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(LunarMissionRegistered e)
        {
            ArrivalTime     = e.ArrivalTime;
            VehicleType     = e.VehicleType;
            CrewManifest    = new List<(string,string)>(e.CrewManifest).AsReadOnly();
            PayloadManifest = new List<(string,double)>(e.PayloadManifest).AsReadOnly();
            Status          = LunarMissionStatus.Registered;
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(DockingPortAssigned e)
        {
            AssignedStation = e.StationId;
            AssignedPort    = e.PortId;
            Status          = LunarMissionStatus.DockingScheduled;
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(LunarMissionDocked e)
        {
            Status = LunarMissionStatus.Docked;
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(CrewTransferred e)
        {
            Status = LunarMissionStatus.Unloaded;
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(PayloadUnloaded e)
        {
            Status = LunarMissionStatus.Unloaded;
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

        protected override ExternalMissionId GetIdFromStringRepresentation(string id)
            => new ExternalMissionId(id);
    }
}
