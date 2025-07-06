// Domain/ValueObjects/MissionId.cs

using DDD.BuildingBlocks.Core.Domain;
using JetBrains.Annotations;

namespace RocketLaunch.SharedKernel.ValueObjects
{
    public class MissionId : EntityId<MissionId>
    {
        public Guid Value { get; }
        
        
        /// <summary>
        /// To support aggregate sourcing, this constructor accepts a string representation of a GUID.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="Exception"></exception>
        [UsedImplicitly]
        public MissionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var guidValue) || guidValue == Guid.Empty)
                throw new Exception("MissionId must be a valid non-empty GUID string");
            Value = guidValue;
        }
        
        public MissionId(Guid value)
        {
            if (value == Guid.Empty)
                throw new Exception("MissionId cannot be empty GUID");
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
    }
}

