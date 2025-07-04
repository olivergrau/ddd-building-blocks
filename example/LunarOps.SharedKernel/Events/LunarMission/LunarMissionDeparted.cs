// Domain/Events/LunarMissionDeparted.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.SharedKernel.Events.LunarMission
{
    [DomainEventType]
    public sealed class LunarMissionDeparted : DomainEvent
    {
        private const int CurrentClassVersion = 1;
        public ExternalMissionId MissionId { get; }

        public LunarMissionDeparted(ExternalMissionId missionId, int targetVersion = -1)
            : base(missionId.Value, targetVersion, CurrentClassVersion)
        {
            MissionId = missionId;
        }
    }
}