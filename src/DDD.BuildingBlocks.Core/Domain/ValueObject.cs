﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DDD.BuildingBlocks.Core.Domain
{
    public abstract class ValueObject<T> where T : ValueObject<T>
    {
        protected abstract IEnumerable<object> GetAttributesToIncludeInEqualityCheck();

        public override bool Equals(object? other)
        {
            return Equals(other as T);
        }

        public bool Equals(T? other = null)
        {
            return other != null && GetAttributesToIncludeInEqualityCheck()
                .SequenceEqual(other.GetAttributesToIncludeInEqualityCheck());
        }

        public static bool operator ==(ValueObject<T>? left, ValueObject<T>? right)
        {
            return StructuralComparisons.StructuralEqualityComparer.Equals(left, right);
        }

        public static bool operator !=(ValueObject<T>? left, ValueObject<T>? right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return GetAttributesToIncludeInEqualityCheck().Aggregate(17,
                (current, obj) => current * 31 + obj.GetHashCode());
        }
    }
}