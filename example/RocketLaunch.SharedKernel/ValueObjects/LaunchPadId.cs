// Domain/ValueObjects/LaunchPadId.cs

using DDD.BuildingBlocks.Core.Domain;

namespace RocketLaunch.SharedKernel.ValueObjects
{
    public class LaunchPadId : EntityId<LaunchPadId>
    {
        public Guid Value { get; }

        public LaunchPadId(Guid value)
        {
            if (value == Guid.Empty)
                throw new Exception("LaunchPadId cannot be empty GUID");
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