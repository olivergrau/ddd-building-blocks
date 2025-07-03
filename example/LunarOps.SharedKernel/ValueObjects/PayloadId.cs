// Domain/ValueObjects/PayloadId.cs

using DDD.BuildingBlocks.Core.Domain;

namespace LunarOps.SharedKernel.ValueObjects
{
    public class PayloadId : EntityId<PayloadId>
    {
        public Guid Value { get; }

        public PayloadId(Guid value)
        {
            if (value == Guid.Empty)
                throw new Exception("PayloadId cannot be empty GUID");
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