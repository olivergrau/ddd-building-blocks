using System;

namespace DDD.BuildingBlocks.Core.Projection;

public sealed record ProjectionKey
{
    public ProjectionKey(string name, int version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);
        Name = name;
        Version = version;
    }

    public string Name { get; }
    public int Version { get; }
}
