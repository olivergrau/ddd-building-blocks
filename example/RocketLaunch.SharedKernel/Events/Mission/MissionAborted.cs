// Domain/Events/MissionAborted.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.SharedKernel.Events.Mission
{
    [DomainEventType]
    public sealed class MissionAborted(
        MissionId missionId,
        int targetVersion = -1) : DomainEvent(missionId.Value.ToString(), targetVersion, CurrentClassVersion)
    {
        private const int CurrentClassVersion = 1;

        public MissionId MissionId { get; } = missionId;
    }
}