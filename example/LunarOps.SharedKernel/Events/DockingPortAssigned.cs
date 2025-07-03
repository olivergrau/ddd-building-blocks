// Domain/Events/DockingPortAssigned.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.SharedKernel.Events
{
    [DomainEventType]
    public sealed class DockingPortAssigned : DomainEvent
    {
        private const int CurrentClassVersion = 1;

        public ExternalMissionId MissionId { get; }
        public StationId         StationId { get; }
        public DockingPortId     PortId    { get; }

        public DockingPortAssigned(
            ExternalMissionId missionId,
            StationId stationId,
            DockingPortId portId,
            int targetVersion = -1
        ) : base(missionId.Value, targetVersion, CurrentClassVersion)
        {
            MissionId = missionId;
            StationId = stationId;
            PortId    = portId;
        }
    }
}