// Domain/Entities/LunarPayload.cs

using DDD.BuildingBlocks.Core.Domain;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.Domain.Model.Entities
{
    public class LunarPayload : Entity<PayloadId>
    {
        public string Description      { get; private set; }
        public double Mass             { get; private set; }
        public string DestinationArea  { get; private set; }
        public bool   IsUnloaded       { get; private set; }

        public LunarPayload(
            PayloadId id,
            string description,
            double mass,
            string destinationArea
        ) : base(id)
        {
            Description     = description;
            Mass            = mass;
            DestinationArea = destinationArea;
            IsUnloaded      = false;
        }

        private LunarPayload() : base(default!) { }

        public void Unload() => IsUnloaded = true;
    }
}