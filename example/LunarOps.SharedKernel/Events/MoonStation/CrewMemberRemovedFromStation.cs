using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.SharedKernel.Events.MoonStation;

[DomainEventType]
public sealed class CrewMemberRemovedFromStation : DomainEvent
{
    private const int CurrentClassVersion = 1;

    public StationId StationId { get; }
    public LunarCrewMemberId CrewMemberId { get; }

    public CrewMemberRemovedFromStation(
        StationId stationId,
        LunarCrewMemberId crewMemberId,
        int targetVersion = -1
    ) : base(stationId.Value.ToString(), targetVersion, CurrentClassVersion)
    {
        StationId = stationId;
        CrewMemberId = crewMemberId;
    }
}