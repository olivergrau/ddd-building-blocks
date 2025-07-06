// Domain/ValueObjects/StationId.cs

using DDD.BuildingBlocks.Core.Domain;
using JetBrains.Annotations;

namespace LunarOps.SharedKernel.ValueObjects
{
    public class StationId : EntityId<StationId>
    {
        public Guid Value { get; }
        
        /// <summary>
        /// To support aggregate sourcing, this constructor accepts a string representation of a GUID.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="Exception"></exception>
        [UsedImplicitly]
        public StationId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var guidValue) || guidValue == Guid.Empty)
                throw new Exception("StationId must be a valid non-empty GUID string");
            Value = guidValue;
        }
        
        public StationId(Guid value)
        {
            if (value == Guid.Empty)
                throw new Exception("StationId cannot be empty GUID");
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