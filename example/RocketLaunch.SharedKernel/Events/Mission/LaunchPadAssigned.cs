// Domain/Events/LaunchPadAssigned.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.SharedKernel.Events.Mission
{
    [DomainEventType]
    public sealed class LaunchPadAssigned : DomainEvent
    {
        private const int CurrentClassVersion = 1;

        public MissionId   MissionId { get; }
        public LaunchPadId PadId     { get; }
        public string      Name            { get; }
        public string      Location        { get; }
        public string[]    SupportedRockets { get; }
        
        public LaunchWindow LaunchWindow { get; }
        
        public LaunchPadAssigned(
            MissionId missionId,
            LaunchPadId padId,
            string name,
            string location,
            string[] supportedRockets,
            LaunchWindow launchWindow,
            int targetVersion = -1
        ) : base(missionId.Value.ToString(), targetVersion, CurrentClassVersion)
        {
            MissionId = missionId;
            PadId     = padId;
            LaunchWindow = launchWindow;
            Name = name;
            Location = location;
            SupportedRockets = supportedRockets ?? throw new ArgumentNullException(nameof(supportedRockets));
        }
    }
}