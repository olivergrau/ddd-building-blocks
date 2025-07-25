using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.SharedKernel.Events.MoonStation
{
    [DomainEventType]
    public sealed class DockingPortReserved : DomainEvent
    {
        private const int CurrentClassVersion = 1;

        public StationId StationId       { get; }
        public DockingPortId PortId          { get; }
        public ExternalMissionId MissionId       { get; }
        public VehicleType VehicleType     { get; }

        public DockingPortReserved(
            StationId stationId,
            DockingPortId portId,
            ExternalMissionId missionId,
            VehicleType vehicleType,
            int targetVersion = -1
        ) : base(stationId.Value.ToString(), targetVersion, CurrentClassVersion)
        {
            StationId   = stationId;
            PortId      = portId;
            MissionId   = missionId;
            VehicleType = vehicleType;
        }
    }
}