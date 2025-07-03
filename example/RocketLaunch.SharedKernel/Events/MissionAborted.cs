// Domain/Events/MissionAborted.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.SharedKernel.Events
{
    [DomainEventType]
    public sealed class MissionAborted : DomainEvent
    {
        private const int CurrentClassVersion = 1;

        public MissionId MissionId { get; }

        public MissionAborted(
            MissionId missionId,
            int targetVersion = -1
        ) : base(missionId.Value.ToString(), targetVersion, CurrentClassVersion)
        {
            MissionId = missionId;
        }
    }
}