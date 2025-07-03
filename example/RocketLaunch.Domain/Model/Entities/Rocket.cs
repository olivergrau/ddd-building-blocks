// Domain/Entities/Rocket.cs

using DDD.BuildingBlocks.Core.Domain;
using JetBrains.Annotations;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Domain.Model.Entities
{
    public class Rocket : Entity<RocketId>
    {
        public Rocket(RocketId id, string name, double thrust, double payloadCapacity, int crewCapacity)
            : base(id)
        {
            Name = name;
            ThrustCapacity = thrust;
            PayloadCapacity = payloadCapacity;
            CrewCapacity = crewCapacity;
            Status = RocketStatus.Available;
        }

        // For rehydration
        [UsedImplicitly]
        private Rocket() : base(default!) { }

        public string      Name            { get; private set; } = null!;
        public double      ThrustCapacity  { get; private set; }
        public double      PayloadCapacity { get; private set; }
        public int         CrewCapacity    { get; private set; }
        public RocketStatus Status         { get; private set; }

        public void MarkAssigned()
        {
            if (Status != RocketStatus.Available)
                throw new Exception("Rocket is not available for assignment");
            Status = RocketStatus.Assigned;
        }

        public void MarkAvailable()
        {
            Status = RocketStatus.Available;
        }
    }
}