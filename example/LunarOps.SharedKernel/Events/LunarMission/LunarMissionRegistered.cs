// Domain/Events/LunarMissionRegistered.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.SharedKernel.Events.LunarMission
{
    [DomainEventType]
    public sealed class LunarMissionRegistered : DomainEvent
    {
        private const int CurrentClassVersion = 1;

        public ExternalMissionId          MissionId       { get; }
        public DateTime                   ArrivalTime     { get; }
        public VehicleType                VehicleType     { get; }
        public StationId AssignedStationId { get; }
        public IReadOnlyCollection<(string Name,string Role)> CrewManifest    { get; }
        public IReadOnlyCollection<(string Item,double Mass)> PayloadManifest { get; }

        public LunarMissionRegistered(
            ExternalMissionId missionId,
            DateTime arrivalTime,
            VehicleType vehicleType,
            IEnumerable<(string Name,string Role)> crewManifest,
            IEnumerable<(string Item,double Mass)> payloadManifest,
            StationId assignedStationId,
            int targetVersion = -1
        ) : base(missionId.ToString(), targetVersion, CurrentClassVersion)
        {
            MissionId       = missionId;
            ArrivalTime     = arrivalTime;
            VehicleType     = vehicleType;
            AssignedStationId = assignedStationId;
            CrewManifest    = new List<(string,string)>(crewManifest).AsReadOnly();
            PayloadManifest = new List<(string,double)>(payloadManifest).AsReadOnly();
        }
    }
}