using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.SharedKernel.Events.LunarMission;

[DomainEventType]
public sealed class PayloadUnloaded : DomainEvent
{
    private const int CurrentClassVersion = 1;

    public string                            MissionId { get; }
    public IReadOnlyCollection<LunarPayload> Payload      { get; }

    public PayloadUnloaded(
        ExternalMissionId missionId,
        IEnumerable<LunarPayload> payload,
        int targetVersion = -1
    ) : base(missionId.Value, targetVersion, CurrentClassVersion)
    {
        MissionId = missionId.Value;
        Payload      = new List<LunarPayload>(payload).AsReadOnly();
    }
}