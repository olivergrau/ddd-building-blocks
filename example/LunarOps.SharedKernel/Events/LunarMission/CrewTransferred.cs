// Domain/Events/CrewTransferred.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.SharedKernel.Events.LunarMission
{
    [DomainEventType]
    public sealed class CrewTransferred : DomainEvent
    {
        private const int CurrentClassVersion = 1;

        public string                            MissionId { get; }
        public IReadOnlyCollection<LunarCrewMemberId> Crew      { get; }

        public CrewTransferred(
            ExternalMissionId missionId,
            IEnumerable<LunarCrewMemberId> crew,
            int targetVersion = -1
        ) : base(missionId.Value, targetVersion, CurrentClassVersion)
        {
            MissionId = missionId.Value;
            Crew      = new List<LunarCrewMemberId>(crew).AsReadOnly();
        }
    }
}