using System;
using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Core.Persistence;

public interface IAggregateInformationService
{
    Task<Type?> GetTypeForAggregateId(string aggregateId);
}