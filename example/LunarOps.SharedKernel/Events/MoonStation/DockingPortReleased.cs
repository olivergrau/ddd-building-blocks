using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.SharedKernel.Events.MoonStation;

[DomainEventType]
public sealed class DockingPortReleased : DomainEvent
{
    private const int CurrentClassVersion = 1;

    public StationId StationId { get; }
    public DockingPortId PortId { get; }

    public DockingPortReleased(
        StationId stationId,
        DockingPortId portId,
        int targetVersion = -1
    ) : base(stationId.Value.ToString(), targetVersion, CurrentClassVersion)
    {
        StationId = stationId;
        PortId = portId;
    }
}