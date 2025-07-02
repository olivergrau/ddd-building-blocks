namespace DDD.BuildingBlocks.Core.Domain
{
    /// <summary>
    ///     Models the concept of a logical entity identifier. There aren't any restrictions, the logical key can be
    ///     composed from multiple sub types.
    ///
    ///     It is also possible to enforce domain constraints on the keys.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class EntityId<T> : ValueObject<EntityId<T>>
    {
    }
}