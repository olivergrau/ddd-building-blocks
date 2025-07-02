namespace DDD.BuildingBlocks.DevelopmentPackage.Storage;

using System;

#pragma warning disable 1998
public class IdMapping
{
    public string Key { get; set; } = default!;
    public Guid PhysicalId { get; set; }
    public Type AggregateType { get; set; } = default!;
}
