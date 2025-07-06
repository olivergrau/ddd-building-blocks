// Domain/Events/LunarMissionDocked.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.SharedKernel.Events.LunarMission
{
    [DomainEventType]
    public sealed class LunarMissionDocked : DomainEvent
    {
        private const int CurrentClassVersion = 1;
        public ExternalMissionId MissionId { get; }

        public LunarMissionDocked(ExternalMissionId missionId, int targetVersion = -1)
            : base(missionId.ToString(), targetVersion, CurrentClassVersion)
        {
            MissionId = missionId;
        }
    }
}