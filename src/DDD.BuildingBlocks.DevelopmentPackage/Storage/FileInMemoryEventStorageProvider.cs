namespace DDD.BuildingBlocks.DevelopmentPackage.Storage;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Event;
using Core.Persistence;
using Core.Util;
using EventPublishing;

#pragma warning disable 1998
public class FileInMemoryEventStorageProvider : PureInMemoryEventStorageProvider
{
    private readonly string _memoryDumpFile;

    private void RefreshFromFiles()
    {
        if (File.Exists(_memoryDumpFile))
        {
            EventStream = SerializerHelper.LoadListFromFile<Dictionary<Guid, List<IDomainEvent>>>(_memoryDumpFile).First();
            UniqueProperties = SerializerHelper.LoadListFromFile<Dictionary<Guid, List<UniquePropertyRow>>>(_memoryDumpFile + "._unique").First();
            IdMapping = SerializerHelper.LoadListFromFile<List<IdMapping>>(_memoryDumpFile + "._mappings").First();
        }
    }

    public FileInMemoryEventStorageProvider(string memoryDumpFile, EventPublishingTable? eventPublishingTable = null)
        : base(eventPublishingTable)
    {
        _memoryDumpFile = memoryDumpFile;

        RefreshFromFiles();
    }

    public override Task<IEnumerable<IDomainEvent>?> GetEventsAsync(Type aggregateType, string key, long start, long count, System.Threading.CancellationToken cancellationToken)
    {
        RefreshFromFiles();
        return base.GetEventsAsync(aggregateType, key, start, count, cancellationToken);
    }

    public override Task<IDomainEvent?> GetLastEventAsync(Type aggregateType, string key, System.Threading.CancellationToken cancellationToken)
    {
        RefreshFromFiles();
        return base.GetLastEventAsync(aggregateType, key, cancellationToken);
    }

    public override async Task CommitChangesAsync(IEventSourcingBasedAggregate aggregate, System.Threading.CancellationToken cancellationToken)
    {
        RefreshFromFiles();

        await base.CommitChangesAsync(aggregate, cancellationToken);

        SerializerHelper.SaveListToFile(_memoryDumpFile, new[] { EventStream });
        SerializerHelper.SaveListToFile(_memoryDumpFile + "._unique", new[] { UniqueProperties });
        SerializerHelper.SaveListToFile(_memoryDumpFile + "._mappings", new[] { IdMapping });

        var events = aggregate.GetUncommittedChanges();
        var enumerable = events as IDomainEvent[] ?? events.ToArray();

        PublishNewEvents(enumerable);
    }
}
