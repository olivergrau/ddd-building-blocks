using System;
using System.Collections.Generic;
using DDD.BuildingBlocks.Core.Exception;

namespace DDD.BuildingBlocks.Core.Persistence.Repository;

public sealed class AggregateTypeRegistry : IAggregateTypeRegistry
{
    private readonly Dictionary<string, Type> _byAggregateType = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, string> _byClrType = [];
    private readonly object _sync = new();

    public AggregateTypeRegistry Register<TAggregate>(string aggregateType) =>
        Register(typeof(TAggregate), aggregateType);

    public AggregateTypeRegistry Register(Type clrType, string aggregateType)
    {
        ArgumentNullException.ThrowIfNull(clrType);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        lock (_sync)
        {
            if (_byAggregateType.TryGetValue(aggregateType, out var registeredClrType))
            {
                throw new AggregateTypeRegistrationException(
                    $"Aggregate type key '{aggregateType}' is already registered for {registeredClrType.FullName}.");
            }
            if (_byClrType.TryGetValue(clrType, out var registeredAggregateType))
            {
                throw new AggregateTypeRegistrationException(
                    $"CLR type {clrType.FullName} is already registered as '{registeredAggregateType}'.");
            }
            _byAggregateType.Add(aggregateType, clrType);
            _byClrType.Add(clrType, aggregateType);
        }
        return this;
    }

    public string GetAggregateType(Type clrType)
    {
        ArgumentNullException.ThrowIfNull(clrType);
        lock (_sync)
        {
            return _byClrType.TryGetValue(clrType, out var aggregateType)
                ? aggregateType
                : throw new UnknownAggregateTypeException(clrType.FullName ?? clrType.Name);
        }
    }
}
