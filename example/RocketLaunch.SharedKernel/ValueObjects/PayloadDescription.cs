// Domain/ValueObjects/PayloadDescription.cs

using DDD.BuildingBlocks.Core.Domain;

namespace RocketLaunch.SharedKernel.ValueObjects
{
    public class PayloadDescription : ValueObject<PayloadDescription>
    {
        public string Value { get; }

        public PayloadDescription(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new Exception("Payload description cannot be empty");
            Value = value.Trim();
        }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            yield return Value;
        }
    }
}