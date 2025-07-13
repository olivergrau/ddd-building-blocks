// Domain/Entities/Rocket.cs

using DDD.BuildingBlocks.Core.Domain;
using JetBrains.Annotations;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Domain.Model.Entities
{
    public class Rocket : Entity<RocketId>
    {
        public Rocket(RocketId id, string name, double thrust, int payloadCapacityKg, int crewCapacity)
            : base(id)
        {
            Name = name;
            ThrustCapacity = thrust;
            PayloadCapacityKg = payloadCapacityKg;
            CrewCapacity = crewCapacity;
            Status = RocketStatus.Available;
        }

        // For rehydration
        [UsedImplicitly]
        private Rocket() : base(default!) { }

        public string      Name            { get; private set; } = null!;
        public double      ThrustCapacity  { get; private set; }
        public int      PayloadCapacityKg { get; private set; }
        public int         CrewCapacity    { get; private set; }
        public RocketStatus Status         { get; private set; }
    }
}