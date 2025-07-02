// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace DDD.BuildingBlocks.Core.Attribute;

using System;

/// <summary>
///     Marks an event as a type for deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DomainEventTypeAttribute(string? category = null) : Attribute
{
    public string? Category { get; } = category;
}
