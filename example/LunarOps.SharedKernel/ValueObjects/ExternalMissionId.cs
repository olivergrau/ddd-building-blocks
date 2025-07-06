// Domain/ValueObjects/ExternalMissionId.cs

using DDD.BuildingBlocks.Core.Domain;

namespace LunarOps.SharedKernel.ValueObjects
{
    public class ExternalMissionId : EntityId<ExternalMissionId>
    {
        public string Value { get; }
        
        public ExternalMissionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new Exception("ExternalMissionId cannot be empty");
            Value = value;
        }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            yield return Value;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}