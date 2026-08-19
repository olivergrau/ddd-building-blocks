using System;

namespace DDD.BuildingBlocks.Core.Persistence.Repository;

public interface IAggregateTypeRegistry
{
    string GetAggregateType(Type clrType);
}
