// Domain/Events/PayloadUnloaded.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.SharedKernel.Events
{
    [DomainEventType]
    public sealed class PayloadUnloaded : DomainEvent
    {
        private const int CurrentClassVersion = 1;

        public string                          MissionId     { get; }
        public IReadOnlyCollection<PayloadId> PayloadItems  { get; }

        public PayloadUnloaded(
            ExternalMissionId missionId,
            IEnumerable<PayloadId> payloadItems,
            int targetVersion = -1
        ) : base(missionId.Value, targetVersion, CurrentClassVersion)
        {
            MissionId    = missionId.Value;
            PayloadItems = new List<PayloadId>(payloadItems).AsReadOnly();
        }
    }
}