namespace DDD.BuildingBlocks.DevelopmentPackage.Service;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Exception;
using Core.Persistence;
using Core.Util;
using Storage;

public class AggregateInformationService : IAggregateInformationService
{
    private readonly string _memoryDumpFile;

    public AggregateInformationService(string memoryDumpFile)
    {
        if (string.IsNullOrWhiteSpace(memoryDumpFile))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(memoryDumpFile));
        }

        _memoryDumpFile = memoryDumpFile;
    }

    public Task<Type?> GetTypeForAggregateId(string aggregateId)
    {
        List<IdMapping> idMapping;
        if (File.Exists(_memoryDumpFile))
        {
            idMapping = SerializerHelper.LoadListFromFile<List<IdMapping>>(_memoryDumpFile + "._mappings")!.First();
        }
        else
        {
            throw new ProviderException("Could not find mapping file");
        }

        if (idMapping.All(q => q.Key != aggregateId))
        {
            return Task.FromResult<Type?>(null);
        }

        return Task.FromResult<Type?>(idMapping.Single(q => q.Key == aggregateId)
            .AggregateType);
    }
}
