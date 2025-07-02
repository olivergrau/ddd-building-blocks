namespace DDD.BuildingBlocks.DevelopmentPackage.Storage;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Attribute;
using Core.Event;
using Core.Exception;
using Core.Persistence;
using DDD.BuildingBlocks.Core.Persistence.Storage;

#pragma warning disable 1998
using System.Diagnostics.CodeAnalysis;
using Core.Domain;
using EventPublishing;

[SuppressMessage("Design", "CA1051:Do not declare visible instance fields")]
public class PureInMemoryEventStorageProvider : IEventStorageProvider
{
    protected Dictionary<Guid, List<UniquePropertyRow>> UniqueProperties = new();

    private readonly EventPublishingTable? _eventPublishingTable;
    protected Dictionary<Guid, List<IDomainEvent>> EventStream = new();
    protected List<IdMapping> IdMapping = [];

    public PureInMemoryEventStorageProvider(EventPublishingTable? eventPublishingTable = null)
    {
        if(eventPublishingTable != null)
        {
            _eventPublishingTable = eventPublishingTable;
        }
    }

    public virtual async Task<IEnumerable<IDomainEvent>?> GetEventsAsync(Type aggregateType, string key, int start,
        int count)
    {
        try
        {
            if (!IdMapping.Any(q => q.AggregateType == aggregateType && q.Key == key))
            {
                return [];
            }

            var aggregateId = IdMapping.Single(q => q.AggregateType == aggregateType && q.Key == key)
                .PhysicalId;

            //this is needed for make sure it doesn't fail when we have int.maxValue for count
            if (count > int.MaxValue - start)
            {
                count = int.MaxValue - start;
            }

            return
                EventStream[aggregateId].Where(
                        o =>
                            EventStream[aggregateId].IndexOf(o) >= start &&
                            EventStream[aggregateId].IndexOf(o) < start + count)
                    .ToArray();
        }
        catch (Exception ex)
        {
            throw new AggregateNotFoundException($"The aggregate with {key} was not found. Details {ex.Message}");
        }
    }

    public virtual async Task<IDomainEvent?> GetLastEventAsync(Type aggregateType, string key)
    {
        if (!IdMapping.Any(q => q.AggregateType == aggregateType && q.Key == key))
        {
            return null;
        }

        var aggregateId = IdMapping.Single(q => q.AggregateType == aggregateType && q.Key == key)
            .PhysicalId;

        return EventStream.ContainsKey(aggregateId) ? EventStream[aggregateId].Last() : null;
    }

    public virtual async Task CommitChangesAsync(IEventSourcingBasedAggregate aggregate)
    {
        var events = aggregate.GetUncommittedChanges();

        var enumerable = events as IDomainEvent[] ?? events.ToArray();
        if (enumerable.Any())
        {
            var uniqueProperties = aggregate.GetType().GetProperties().Where(
                prop => Attribute.IsDefined(prop, typeof(UniqueDomainPropertyAttribute)));

            var existingId = Guid.Empty;
            var newId = Guid.NewGuid();

            if (IdMapping.Any(q => q.AggregateType == aggregate.GetType() && q.Key == aggregate.SerializedId))
            {
                existingId = IdMapping
                    .Single(q => q.AggregateType == aggregate.GetType() && q.Key == aggregate.SerializedId).PhysicalId;
            }

            if (aggregate.GetStreamState() == StreamState.StreamClosed)
            {
                if (UniqueProperties.ContainsKey(existingId))
                {
                    UniqueProperties.Remove(existingId);
                }
            }
            else
            {
                foreach (var uniqueProperty in uniqueProperties)
                {
                    if (uniqueProperty.GetValue(aggregate) == null)
                    {
                        continue; // We allow unique properties to be null any number of times
                    }

                    var uniquePropertyValue = uniqueProperty.GetValue(aggregate)?.ToString();

                    if (existingId != Guid.Empty && UniqueProperties.ContainsKey(existingId))
                    {
                        var properties = UniqueProperties[existingId];

                        if (properties.Union(UniqueProperties.SelectMany(s => s.Value)).Any(p =>
                                p.Property == uniqueProperty.Name && p.Value == uniquePropertyValue && p.Id != existingId))
                        {
                            throw new ProviderException($"A unique constraint has been violated. [{uniqueProperty.Name}]");
                        }

                        var propertyRow =
                            properties.FirstOrDefault(p => p.Property == uniqueProperty.Name && p.Id == existingId);

                        if (propertyRow != null && uniquePropertyValue != null)
                        {
                            propertyRow.Value = uniquePropertyValue;
                        }
                    }
                    else
                    {
                        if (!UniqueProperties.ContainsKey(newId))
                        {
                            UniqueProperties.Add(newId, []);
                        }

                        var properties = UniqueProperties[newId];

                        if (properties.Union(UniqueProperties.SelectMany(s => s.Value)).Any(p =>
                                p.Property == uniqueProperty.Name && p.Value == uniquePropertyValue))
                        {
                            throw new ProviderException($"A unique constraint has been violated. [{uniqueProperty.Name}]");
                        }

                        if(uniquePropertyValue != null)
                        {
                            properties.Add(new UniquePropertyRow
                            {
                                Id = newId, AggregateType = aggregate.GetType(), Property = uniqueProperty.Name,
                                Value = uniquePropertyValue
                            });
                        }
                    }
                }
            }

            if (EventStream.ContainsKey(existingId) == false)
            {
                EventStream.Add(newId, enumerable.ToList());
                IdMapping.Add(new IdMapping
                    { AggregateType = aggregate.GetType(), Key = aggregate.SerializedId, PhysicalId = newId });
            }
            else
            {
                EventStream[existingId].AddRange(enumerable);
            }
        }

        PublishNewEvents(enumerable);
    }

    protected virtual void PublishNewEvents(IDomainEvent[] enumerable)
    {
        if (_eventPublishingTable != null)
        {
            foreach (var domainEvent in enumerable)
            {
                _eventPublishingTable.Enqueue(domainEvent);
            }
        }
    }
}
