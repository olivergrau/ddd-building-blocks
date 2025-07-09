// Domain/ValueObjects/CrewMemberId.cs

using DDD.BuildingBlocks.Core.Domain;
using JetBrains.Annotations;

namespace RocketLaunch.SharedKernel.ValueObjects
{
    public class CrewMemberId : EntityId<CrewMemberId>
    {
        public Guid Value { get; }

        /// <summary>
        /// To support aggregate sourcing, this constructor accepts a string representation of a GUID.
        /// </summary>
        [UsedImplicitly]
        public CrewMemberId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var guidValue) || guidValue == Guid.Empty)
                throw new Exception("CrewMemberId must be a valid non-empty GUID string");
            Value = guidValue;
        }

        public CrewMemberId(Guid value)
        {
            if (value == Guid.Empty)
                throw new Exception("CrewMemberId cannot be empty GUID");
            Value = value;
        }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            yield return Value;
        }
        
        public override string ToString() 
        {
            return Value.ToString();
        }
    }}