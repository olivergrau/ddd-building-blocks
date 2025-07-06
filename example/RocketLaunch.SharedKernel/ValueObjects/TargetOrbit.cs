// Domain/ValueObjects/TargetOrbit.cs

using DDD.BuildingBlocks.Core.Domain;

namespace RocketLaunch.SharedKernel.ValueObjects
{
    public class TargetOrbit : ValueObject<TargetOrbit>
    {
        public string Value { get; }

        public TargetOrbit(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new Exception("Target orbit cannot be empty");
            Value = value.Trim();
        }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            yield return Value;
        }
    }
}