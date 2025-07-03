// Domain/ValueObjects/VehicleType.cs

using DDD.BuildingBlocks.Core.Domain;

namespace LunarOps.SharedKernel.ValueObjects
{
    public class VehicleType : ValueObject<VehicleType>
    {
        public string Value { get; }

        public VehicleType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new Exception("VehicleType cannot be empty");
            Value = value.Trim();
        }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            yield return Value;
        }
    }
}