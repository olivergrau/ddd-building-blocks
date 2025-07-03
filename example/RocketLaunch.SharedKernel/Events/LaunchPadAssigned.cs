// Domain/Events/LaunchPadAssigned.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.SharedKernel.Events
{
    [DomainEventType]
    public sealed class LaunchPadAssigned : DomainEvent
    {
        private const int CurrentClassVersion = 1;

        public MissionId   MissionId { get; }
        public LaunchPadId PadId     { get; }

        public LaunchPadAssigned(
            MissionId missionId,
            LaunchPadId padId,
            int targetVersion = -1
        ) : base(missionId.Value.ToString(), targetVersion, CurrentClassVersion)
        {
            MissionId = missionId;
            PadId     = padId;
        }
    }
}