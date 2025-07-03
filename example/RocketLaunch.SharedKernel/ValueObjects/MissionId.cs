// Domain/ValueObjects/MissionId.cs

using DDD.BuildingBlocks.Core.Domain;

namespace RocketLaunch.SharedKernel.ValueObjects
{
    public class MissionId : EntityId<MissionId>
    {
        public Guid Value { get; }

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

