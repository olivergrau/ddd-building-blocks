using System.Collections.Generic;
using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Core.Persistence;

public interface IEventSourcingBasedAggregate
{
    string SerializedId { get; }
    
    int CurrentVersion { get; }
    
    int LastCommittedVersion { get; }
    
    StreamState GetStreamState();
    
    bool HasUncommittedChanges();
    
    IEnumerable<IDomainEvent> GetUncommittedChanges();
    
    void MarkChangesAsCommitted();
    
    void ReplayEvents(IEnumerable<IDomainEvent> history);
}