// Domain/Events/RocketAssigned.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Event;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.SharedKernel.Events.Mission
{
    [DomainEventType]
    public sealed class RocketAssigned : DomainEvent
    {
        private const int CurrentClassVersion = 1;

        public MissionId MissionId { get; }
        public RocketId  RocketId  { get; }
        
        public string      Name            { get; }
        public double      ThrustCapacity  { get; }
        public int      PayloadCapacityKg { get; }
        public int         CrewCapacity    { get; }
        
        public RocketAssigned(
            MissionId missionId,
            RocketId rocketId,
            string name,
            double thrustCapacity,
            int payloadCapacityKg,
            int crewCapacity,
            int targetVersion = -1
        ) : base(missionId.Value.ToString(), targetVersion, CurrentClassVersion)
        {
            MissionId = missionId;
            RocketId  = rocketId;
            Name            = name;
            ThrustCapacity  = thrustCapacity;
            PayloadCapacityKg = payloadCapacityKg;
            CrewCapacity    = crewCapacity;
        }
    }
}