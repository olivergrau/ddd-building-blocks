// Domain/Events/MissionCreated.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.SharedKernel.Events.Mission
{
    [DomainEventType]
    public sealed class MissionCreated : DomainEvent
    {
        private const int CurrentClassVersion = 1;

        public MissionId           MissionId    { get; }
        public MissionName         Name         { get; }
        public TargetOrbit         TargetOrbit  { get; }
        public PayloadDescription  Payload      { get; }
        public LaunchWindow        LaunchWindow { get; }

        public MissionCreated(
            MissionId missionId,
            MissionName name,
            TargetOrbit targetOrbit,
            PayloadDescription payload,
            LaunchWindow launchWindow
        ) : base(missionId.Value.ToString(), -1, CurrentClassVersion)
        {
            MissionId    = missionId;
            Name         = name;
            TargetOrbit  = targetOrbit;
            Payload      = payload;
            LaunchWindow = launchWindow;
        }
    }
}