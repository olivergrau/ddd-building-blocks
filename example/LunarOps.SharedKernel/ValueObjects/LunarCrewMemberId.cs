// Domain/ValueObjects/LunarCrewMemberId.cs

using DDD.BuildingBlocks.Core.Domain;

namespace LunarOps.SharedKernel.ValueObjects
{
    public class LunarCrewMemberId : EntityId<LunarCrewMemberId>
    {
        public Guid Value { get; }

        public LunarCrewMemberId(Guid value)
        {
            if (value == Guid.Empty)
                throw new Exception("LunarCrewMemberId cannot be empty GUID");
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