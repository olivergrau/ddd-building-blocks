using System;
using System.Collections.Generic;
using DDD.BuildingBlocks.Core.Domain;

namespace DDD.BuildingBlocks.Tests.Abstracts.Model
{
    public class CompoundEntityKey(Guid keyA, string keyB) : EntityId<CompoundEntityKey>
    {
        public Guid KeyA { get; } = keyA;
        public string KeyB { get; } = keyB;

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            return new List<object> { KeyA, KeyB };
        }

        public override string ToString()
        {
            return $"{KeyA.ToString()}:{KeyB}";
        }
    }
}