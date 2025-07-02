using System;
using System.Collections.Generic;
using DDD.BuildingBlocks.Core.Domain;

namespace DDD.BuildingBlocks.Tests.Abstracts.Model
{
    public class StringBasedEntityKey : EntityId<StringBasedEntityKey>
    {
        public string Key { get; }

        public StringBasedEntityKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
            }

            Key = key;
        }

        public static StringBasedEntityKey GetNewId(string id)
        {
            return new StringBasedEntityKey(id);
        }

        public override string ToString()
        {
            return Key;
        }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            return new List<object> { Key };
        }
    }
}