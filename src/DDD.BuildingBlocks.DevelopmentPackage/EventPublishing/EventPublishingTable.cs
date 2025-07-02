namespace DDD.BuildingBlocks.DevelopmentPackage.EventPublishing;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Core.Event;

public class EventPublishingTable
{
    private List<string> _alreadyPublished = [];
    public ConcurrentDictionary<string, ConcurrentQueue<IDomainEvent>> WorkerQueues { get; } = new();

    public void RegisterWorkerId(string workerId)
    {
        if (WorkerQueues.ContainsKey(workerId))
        {
            throw new InvalidOperationException("Already registered workerId");
        }

        WorkerQueues[workerId] = new ConcurrentQueue<IDomainEvent>();
    }

    public void Enqueue(IDomainEvent data)
    {
        var eventKey = $"{data.SerializedAggregateId}-{data.TargetVersion}";

        if(!_alreadyPublished.Contains(eventKey))
        {
            foreach (var queue in WorkerQueues.Select(p => p.Value))
            {
                queue.Enqueue(data);
            }

            _alreadyPublished.Add(eventKey);
        }
    }

    public IDomainEvent? Dequeue(string workerId)
    {
        if (!WorkerQueues.ContainsKey(workerId))
        {
            throw new InvalidOperationException("Invalid key");
        }

        if (WorkerQueues[workerId]
            .TryDequeue(out var elem))
        {
            return elem;
        }

        return null;
    }
}
