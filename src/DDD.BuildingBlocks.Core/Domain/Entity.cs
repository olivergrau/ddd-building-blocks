using System;

namespace DDD.BuildingBlocks.Core.Domain
{
    public abstract class Entity<TKey>(TKey id) : IEquatable<Entity<TKey>>
        where TKey : EntityId<TKey>
    {
        public TKey Id { get; protected set; } = id;

        public string SerializedId => Id.ToString()!;
        public override bool Equals(object? obj)
        {
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return obj is Entity<TKey> entity ? Equals(entity) : base.Equals(obj);
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return Id.GetHashCode();
        }

        public bool Equals(Entity<TKey>? other)
        {
            return other != null && Id.Equals(other.Id);
        }
    }
}