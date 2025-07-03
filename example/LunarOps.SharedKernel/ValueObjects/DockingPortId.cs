// Domain/ValueObjects/DockingPortId.cs

using DDD.BuildingBlocks.Core.Domain;

namespace LunarOps.SharedKernel.ValueObjects
{
    public class DockingPortId : EntityId<DockingPortId>
    {
        public Guid Value { get; }

        public DockingPortId(Guid value)
        {
            if (value == Guid.Empty)
                throw new Exception("DockingPortId cannot be empty GUID");
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