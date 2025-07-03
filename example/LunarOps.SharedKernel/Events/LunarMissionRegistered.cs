// Domain/Events/LunarMissionRegistered.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.SharedKernel.Events
{
    [DomainEventType]
    public sealed class LunarMissionRegistered : DomainEvent
    {
        private const int CurrentClassVersion = 1;

        public ExternalMissionId          MissionId       { get; }
        public DateTime                   ArrivalTime     { get; }
        public VehicleType                VehicleType     { get; }
        public IReadOnlyCollection<(string Name,string Role)> CrewManifest    { get; }
        public IReadOnlyCollection<(string Item,double Mass)> PayloadManifest { get; }

        public LunarMissionRegistered(
            ExternalMissionId missionId,
            DateTime arrivalTime,
            VehicleType vehicleType,
            IEnumerable<(string Name,string Role)> crewManifest,
            IEnumerable<(string Item,double Mass)> payloadManifest,
            int targetVersion = -1
        ) : base(missionId.Value, targetVersion, CurrentClassVersion)
        {
            MissionId       = missionId;
            ArrivalTime     = arrivalTime;
            VehicleType     = vehicleType;
            CrewManifest    = new List<(string,string)>(crewManifest).AsReadOnly();
            PayloadManifest = new List<(string,double)>(payloadManifest).AsReadOnly();
        }
    }
}