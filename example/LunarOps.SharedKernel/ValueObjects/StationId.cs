// Domain/ValueObjects/StationId.cs

using DDD.BuildingBlocks.Core.Domain;

namespace LunarOps.SharedKernel.ValueObjects
{
    public class StationId : EntityId<StationId>
    {
        public Guid Value { get; }

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