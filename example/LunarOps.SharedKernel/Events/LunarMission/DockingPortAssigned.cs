// Domain/Events/DockingPortAssigned.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.SharedKernel.Events.LunarMission
{
    [DomainEventType]
    public sealed class DockingPortAssigned : DomainEvent
    {
        private const int CurrentClassVersion = 1;

        public ExternalMissionId MissionId { get; }
        public DockingPortId     PortId    { get; }

        public DockingPortAssigned(
            ExternalMissionId missionId,
            DockingPortId portId,
            int targetVersion = -1
        ) : base(missionId.ToString(), targetVersion, CurrentClassVersion)
        {
            MissionId = missionId;
            PortId    = portId;
        }
    }
}