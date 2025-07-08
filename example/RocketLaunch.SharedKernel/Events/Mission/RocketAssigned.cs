// Domain/Events/RocketAssigned.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.SharedKernel.Events.Mission
{
    [DomainEventType]
    public sealed class RocketAssigned : DomainEvent
    {
        private const int CurrentClassVersion = 1;

        public MissionId MissionId { get; }
        public RocketId  RocketId  { get; }

        public RocketAssigned(
            MissionId missionId,
            RocketId rocketId,
            int targetVersion = -1
        ) : base(missionId.Value.ToString(), targetVersion, CurrentClassVersion)
        {
            MissionId = missionId;
            RocketId  = rocketId;
        }
    }
}