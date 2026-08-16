using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Persistence;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.Core.Persistence.Storage;
using DDD.BuildingBlocks.Tests.Abstracts.Model;
using FluentAssertions;
using Xunit;

namespace DDD.BuildingBlocks.Core.Tests.Unit;

public sealed class EventSourcingRepositoryShould
{
    [Fact(DisplayName = "Preserve uncommitted events when storage commit fails")]
    [Trait("Category", "Unittest")]
    public async Task Preserve_uncommitted_events_when_storage_commit_fails()
    {
        var aggregate = new Order(Guid.NewGuid().ToString(), "Title", "Comment", OrderState.Open);
        var repository = new EventSourcingRepository(new FailingEventStorageProvider());

        Func<Task> save = () => repository.SaveAsync(aggregate);

        await save.Should().ThrowAsync<InvalidOperationException>();
        aggregate.UncommittedChanges.Should().ContainSingle();
        aggregate.LastCommittedVersion.Should().Be(-1);
    }

    private sealed class FailingEventStorageProvider : IEventStorageProvider
    {
        public Task<IEnumerable<IDomainEvent>?> GetEventsAsync(Type aggregateType, string key, long start, long count)
        {
            return Task.FromResult<IEnumerable<IDomainEvent>?>(null);
        }

        public Task<IDomainEvent?> GetLastEventAsync(Type aggregateType, string key)
        {
            return Task.FromResult<IDomainEvent?>(null);
        }

        public Task CommitChangesAsync(IEventSourcingBasedAggregate aggregate)
        {
            throw new InvalidOperationException("Simulated storage failure.");
        }
    }
}
