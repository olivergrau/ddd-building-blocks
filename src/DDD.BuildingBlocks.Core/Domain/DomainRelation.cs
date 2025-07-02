using System.Collections.Generic;

namespace DDD.BuildingBlocks.Core.Domain
{
    public sealed class DomainRelation(string aggregateId) : ValueObject<DomainRelation>
    {
        public string AggregateId { get; } = aggregateId;

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            return new List<object> { AggregateId };
        }
    }
}