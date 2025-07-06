// Domain/ValueObjects/MissionName.cs

using DDD.BuildingBlocks.Core.Domain;

namespace RocketLaunch.SharedKernel.ValueObjects
{
    public class MissionName : ValueObject<MissionName>
    {
        public string Value { get; }

        public MissionName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new Exception("Mission name cannot be empty");
            Value = value.Trim();
        }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            yield return Value;
        }
    }
}