// Domain/ValueObjects/RocketId.cs

using DDD.BuildingBlocks.Core.Domain;

namespace RocketLaunch.SharedKernel.ValueObjects
{
    public class RocketId : EntityId<RocketId>
    {
        public Guid Value { get; }

        public RocketId(Guid value)
        {
            if (value == Guid.Empty)
                throw new Exception("RocketId cannot be empty GUID");
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