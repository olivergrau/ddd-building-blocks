// Domain/ValueObjects/CrewMemberId.cs

using DDD.BuildingBlocks.Core.Domain;

namespace RocketLaunch.SharedKernel.ValueObjects
{
    public class CrewMemberId : EntityId<CrewMemberId>
    {
        public Guid Value { get; }

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
    }
}