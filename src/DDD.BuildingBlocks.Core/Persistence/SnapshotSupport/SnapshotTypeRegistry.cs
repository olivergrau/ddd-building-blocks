using System;
using System.Collections.Generic;

namespace DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;

public sealed record SnapshotTypeRegistration(string SnapshotType, Type ClrType, int CurrentSchemaVersion);

public sealed class SnapshotTypeRegistry
{
    private readonly Dictionary<string, SnapshotTypeRegistration> _byKey = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, SnapshotTypeRegistration> _byType = [];

    public SnapshotTypeRegistry Register<TSnapshot>(string snapshotType, int currentSchemaVersion = 1)
        where TSnapshot : Snapshot
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotType);
        ArgumentOutOfRangeException.ThrowIfLessThan(currentSchemaVersion, 1);
        var registration = new SnapshotTypeRegistration(snapshotType, typeof(TSnapshot), currentSchemaVersion);
        if (!_byKey.TryAdd(snapshotType, registration) || !_byType.TryAdd(typeof(TSnapshot), registration))
        {
            throw new InvalidOperationException($"Snapshot type '{snapshotType}' or CLR type '{typeof(TSnapshot).FullName}' is already registered.");
        }

        return this;
    }

    public SnapshotTypeRegistration? FindByKey(string snapshotType) =>
        _byKey.TryGetValue(snapshotType, out var registration) ? registration : null;

    public SnapshotTypeRegistration GetByType(Type type) =>
        _byType.TryGetValue(type, out var registration)
            ? registration
            : throw new InvalidOperationException($"Snapshot CLR type '{type.FullName}' is not registered.");
}
