// Domain/ValueObjects/LunarPayload.cs

using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Exception;
using JetBrains.Annotations;

namespace LunarOps.SharedKernel.ValueObjects
{
    public class LunarPayload : ValueObject<LunarPayload>
    {
        public string Description       { get; }
        public double Mass              { get; }
        public string DestinationArea   { get; }

        public LunarPayload(
            string description,
            double mass,
            string destinationArea)
        {
            if (mass <= 0)
                throw new ValueObjectValidationException(nameof(LunarPayload), nameof(Mass), Mass, "Payload mass must be positive.");
            if (string.IsNullOrWhiteSpace(destinationArea))
                throw new ValueObjectValidationException(nameof(LunarPayload), nameof(DestinationArea), "","Destination area must be provided.");
            
            if (string.IsNullOrWhiteSpace(description))
                throw new ValueObjectValidationException(nameof(LunarPayload), nameof(Description), "","Description must be provided.");

            Description     = description;
            Mass            = mass;
            DestinationArea = destinationArea;
        }

        [UsedImplicitly]
        private LunarPayload() { }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            yield return Description;
            yield return Mass;
            yield return DestinationArea;
        }
    }
}