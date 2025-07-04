using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.SharedKernel.Events.MoonStation;

[DomainEventType]
public sealed class CrewMemberAssignedToStation : DomainEvent
{
    private const int CurrentClassVersion = 1;

    public StationId StationId { get; }
    public LunarCrewMemberId CrewMemberId { get; }
    public string Name { get; }
    public string Role { get; }

    public CrewMemberAssignedToStation(
        StationId stationId,
        LunarCrewMemberId crewMemberId,
        string name,
        string role,
        int targetVersion = -1
    ) : base(stationId.Value.ToString(), targetVersion, CurrentClassVersion)
    {
        StationId = stationId;
        CrewMemberId = crewMemberId;
        Name = name;
        Role = role;
    }
}