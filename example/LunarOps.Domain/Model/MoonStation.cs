// Domain/Aggregates/MoonStation.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Exception;
using JetBrains.Annotations;
using LunarOps.Domain.Model.Entities;
using LunarOps.SharedKernel.Enums;
using LunarOps.SharedKernel.Events.MoonStation;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.Domain.Model
{
    // ToDo: Add saving of payloads and crew members to the station (manifests)
    public class MoonStation : AggregateRoot<StationId>
    {
        // Core Info
        public string Name { get; private set; }
        public string Location { get; private set; }
        public StationStatus OperationalStatus { get; private set; }

        // Supported Config
        private readonly HashSet<VehicleType> _supportedVehicleTypes;
        public IReadOnlyCollection<VehicleType> SupportedVehicleTypes => _supportedVehicleTypes;

        // Capacity
        public int MaxCrewCapacity { get; private set; }
        public double MaxPayloadCapacity { get; private set; }

        // State
        private readonly List<DockingPort> _dockingPorts = new();
        public IReadOnlyCollection<DockingPort> DockingPorts => _dockingPorts;

        private readonly List<LunarCrewMember> _crewQuarters = new();
        public IReadOnlyCollection<LunarCrewMember> CrewQuarters => _crewQuarters;

        private readonly List<LunarPayload> _storedPayloads = new();
        public IReadOnlyCollection<LunarPayload> StoredPayloads => _storedPayloads;

        // Constructor
        public MoonStation(
            StationId id,
            string name,
            string location,
            StationStatus status,
            IEnumerable<VehicleType> supportedVehicleTypes,
            int maxCrewCapacity,
            double maxPayloadCapacity,
            IEnumerable<DockingPort> dockingPorts
        ) : base(id)
        {
            Name = name;
            Location = location;
            OperationalStatus = status;
            _supportedVehicleTypes = new HashSet<VehicleType>(supportedVehicleTypes);
            MaxCrewCapacity = maxCrewCapacity;
            MaxPayloadCapacity = maxPayloadCapacity;
            _dockingPorts = new List<DockingPort>(dockingPorts);
        }

        [UsedImplicitly]
        private MoonStation() : base(default!) { }

        // Business Methods

        public DockingPortId ReserveDockingPort(ExternalMissionId missionId, VehicleType vehicleType)
        {
            if (!_supportedVehicleTypes.Contains(vehicleType))
                throw new AggregateValidationException(Id, nameof(SupportedVehicleTypes), vehicleType, "Vehicle type not supported by this station.");

            var availablePort = _dockingPorts.FirstOrDefault(p => p.Status == DockingPortStatus.Available);
            if (availablePort == null)
                throw new AggregateValidationException(Id, nameof(DockingPorts), null, "No available docking ports.");

            ApplyEvent(new DockingPortReserved(Id, availablePort.Id, missionId, vehicleType, CurrentVersion));

            return availablePort.Id;
        }

        public void ReleaseDockingPort(DockingPortId portId)
        {
            var port = _dockingPorts.FirstOrDefault(p => p.Id == portId);
            if (port == null)
                throw new AggregateValidationException(Id, nameof(DockingPorts), portId, "Docking port not found.");

            ApplyEvent(new DockingPortReleased(Id, portId, CurrentVersion));
        }

        public void AssignCrewMember(LunarCrewMember member)
        {
            if (_crewQuarters.Count >= MaxCrewCapacity)
                throw new AggregateValidationException(Id, nameof(CrewQuarters), member.Name, "Station crew capacity exceeded.");

            ApplyEvent(new CrewMemberAssignedToStation(Id, member.Id, member.Name, member.Role, CurrentVersion));
        }

        public void StorePayload(LunarPayload payload)
        {
            var currentMass = _storedPayloads.Sum(p => p.Mass);
            if (currentMass + payload.Mass > MaxPayloadCapacity)
                throw new AggregateValidationException(Id, nameof(StoredPayloads), payload.Mass, "Payload capacity exceeded.");

            ApplyEvent(new PayloadStoredAtStation(Id, payload.Description, payload.Mass, payload.DestinationArea, CurrentVersion));
        }
        
        public void RemoveCrewMember(LunarCrewMemberId crewMemberId)
        {
            var member = _crewQuarters.FirstOrDefault(m => m.Id == crewMemberId);
            if (member == null)
                throw new AggregateValidationException(Id, nameof(CrewQuarters), crewMemberId, "Crew member not found.");

            ApplyEvent(new CrewMemberRemovedFromStation(Id, crewMemberId, CurrentVersion));
        }

        public void RemovePayload(LunarPayload payload)
        {
            if (!_storedPayloads.Any(p =>
                    p.Description == payload.Description &&
                    Math.Abs(p.Mass - payload.Mass) < 0.01 &&
                    p.DestinationArea == payload.DestinationArea))
            {
                throw new AggregateValidationException(Id, nameof(StoredPayloads), payload.Description, "Matching payload not found.");
            }

            ApplyEvent(new PayloadRemovedFromStation(Id, payload.Description, payload.Mass, payload.DestinationArea, CurrentVersion));
        }
        
        // Event Handlers

        [UsedImplicitly]
        [InternalEventHandler]
        private void On(DockingPortReserved e)
        {
            var port = _dockingPorts.Single(p => p.Id == e.PortId);
            port.Occupy(e.VehicleType);
        }

        [UsedImplicitly]
        [InternalEventHandler]
        private void On(DockingPortReleased e)
        {
            var port = _dockingPorts.Single(p => p.Id == e.PortId);
            port.Release();
        }

        [UsedImplicitly]
        [InternalEventHandler]
        private void On(CrewMemberAssignedToStation e)
        {
            var member = new LunarCrewMember(e.CrewMemberId, e.Name, e.Role);
            member.Activate();
            _crewQuarters.Add(member);
        }

        [UsedImplicitly]
        [InternalEventHandler]
        private void On(PayloadStoredAtStation e)
        {
            var payload = new LunarPayload(e.Description, e.Mass, e.DestinationArea);
            _storedPayloads.Add(payload);
        }
        
        [UsedImplicitly]
        [InternalEventHandler]
        private void On(CrewMemberRemovedFromStation e)
        {
            var member = _crewQuarters.FirstOrDefault(m => m.Id == e.CrewMemberId);
            if (member != null)
                _crewQuarters.Remove(member);
        }

        [UsedImplicitly]
        [InternalEventHandler]
        private void On(PayloadRemovedFromStation e)
        {
            var match = _storedPayloads.FirstOrDefault(p =>
                p.Description == e.Description &&
                Math.Abs(p.Mass - e.Mass) < 0.01 &&
                p.DestinationArea == e.DestinationArea);
            if (match != null)
                _storedPayloads.Remove(match);
        }
        
        protected override StationId GetIdFromStringRepresentation(string id)
            => new StationId(Guid.Parse(id));
    }
}
