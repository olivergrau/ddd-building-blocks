namespace DDD.BuildingBlocks.DevelopmentPackage.Storage;

using System;

#pragma warning disable 1998
public class UniquePropertyRow
{
    public Guid Id { get; set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    public Type AggregateType { get; set; } = default!;
    public string Property { get; set; } = default!;
    public string Value { get; set; } = default!;
}
