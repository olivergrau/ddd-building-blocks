// Domain/Events/MissionScheduled.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.SharedKernel.Events.Mission
{
    [DomainEventType]
    public sealed class MissionScheduled : DomainEvent
    {
        private const int CurrentClassVersion = 1;

        public MissionId MissionId { get; }

        public MissionScheduled(
            MissionId missionId,
            int targetVersion = -1
        ) : base(missionId.Value.ToString(), targetVersion, CurrentClassVersion)
        {
            MissionId = missionId;
        }
    }
}