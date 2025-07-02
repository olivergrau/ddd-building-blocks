using System;

namespace DDD.BuildingBlocks.Core.Attribute
{
    /// <summary>
    ///     This attribute must be applied to all aggregate properties which must be unique when persisted.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class UniqueDomainPropertyAttribute : System.Attribute
    {
    }
}
