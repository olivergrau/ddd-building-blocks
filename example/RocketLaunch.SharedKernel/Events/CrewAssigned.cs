// Domain/Events/CrewAssigned.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.SharedKernel.Events
{
    [DomainEventType]
    public sealed class CrewAssigned : DomainEvent
    {
        private const int CurrentClassVersion = 1;

        public MissionId                   MissionId { get; }
        public IReadOnlyCollection<CrewMemberId> Crew      { get; }

        public CrewAssigned(
            MissionId missionId,
            IEnumerable<CrewMemberId> crew,
            int targetVersion = -1
        ) : base(missionId.Value.ToString(), targetVersion, CurrentClassVersion)
        {
            MissionId = missionId;
            Crew      = new List<CrewMemberId>(crew).AsReadOnly();
        }
    }
}