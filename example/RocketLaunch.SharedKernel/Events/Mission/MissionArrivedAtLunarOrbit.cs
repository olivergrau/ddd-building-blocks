// Domain/Events/MissionArrivedAtLunarOrbit.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.SharedKernel.Events.Mission
{
    [DomainEventType(category:"Integration")]
    public sealed class MissionArrivedAtLunarOrbit : DomainEvent
    {
        private const int CurrentClassVersion = 1;

        public MissionId MissionId { get; }
        public DateTime  ArrivalTime { get; }
        public string    VehicleType { get; }
        public IReadOnlyCollection<(string Name, string Role)> CrewManifest    { get; }
        public IReadOnlyCollection<(string Item, double Mass)> PayloadManifest { get; }

        public MissionArrivedAtLunarOrbit(
            MissionId missionId,
            DateTime arrivalTime,
            string vehicleType,
            IEnumerable<(string Name, string Role)> crewManifest,
            IEnumerable<(string Item, double Mass)> payloadManifest,
            int targetVersion = -1
        ) : base(missionId.Value.ToString(), targetVersion, CurrentClassVersion)
        {
            MissionId       = missionId;
            ArrivalTime     = arrivalTime;
            VehicleType     = vehicleType;
            CrewManifest    = new List<(string, string)>(crewManifest).AsReadOnly();
            PayloadManifest = new List<(string, double)>(payloadManifest).AsReadOnly();
        }
    }
}