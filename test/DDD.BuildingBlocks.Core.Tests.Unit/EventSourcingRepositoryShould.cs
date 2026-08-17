using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Persistence;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;
using DDD.BuildingBlocks.Core.Persistence.Storage;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
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
        var repository = new EventSourcingRepository(
            new FailingEventStoreProvider(),
            DDD.BuildingBlocks.Tests.Abstracts.Event.TestEventCodec.Create());

        Func<Task> save = () => repository.SaveAsync(aggregate, System.Threading.CancellationToken.None);

        await save.Should().ThrowAsync<InvalidOperationException>();
        aggregate.UncommittedChanges.Should().ContainSingle();
        aggregate.LastCommittedVersion.Should().Be(-1);
    }

    [Fact(DisplayName = "Keep a successful event commit when snapshot persistence fails")]
    [Trait("Category", "Unittest")]
    public async Task Keep_a_successful_event_commit_when_snapshot_persistence_fails()
    {
        var eventStore = new InMemoryEventStoreProvider();
        var eventCodec = DDD.BuildingBlocks.Tests.Abstracts.Event.TestEventCodec.Create();
        var aggregate = new Order(Guid.NewGuid().ToString(), "Title", "Comment", OrderState.Open);
        var repository = new EventSourcingRepository(
            eventStore,
            eventCodec,
            new FailingSnapshotStoreProvider(),
            DDD.BuildingBlocks.Tests.Abstracts.Snapshot.TestSnapshotCodec.Create());

        await repository.SaveAsync(aggregate, CancellationToken.None);

        aggregate.UncommittedChanges.Should().BeEmpty();
        aggregate.LastCommittedVersion.Should().Be(0);
        var reloaded = await new EventSourcingRepository(eventStore, eventCodec)
            .GetByIdAsync<Order, OrderId>(aggregate.Id, CancellationToken.None);
        reloaded.Should().NotBeNull();
    }

    [Fact(DisplayName = "Reject an incomplete snapshot configuration")]
    [Trait("Category", "Unittest")]
    public void Reject_an_incomplete_snapshot_configuration()
    {
        var create = () => new EventSourcingRepository(
            new InMemoryEventStoreProvider(),
            DDD.BuildingBlocks.Tests.Abstracts.Event.TestEventCodec.Create(),
            new FailingSnapshotStoreProvider());

        create.Should().Throw<ArgumentException>();
    }

    private sealed class FailingEventStoreProvider : IEventStoreProvider
    {
        public Task<AppendEventsResult> AppendAsync(
            string streamId,
            string aggregateType,
            long expectedVersion,
            IReadOnlyCollection<EventEnvelope> events,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Simulated storage failure.");
        }

        public Task<IReadOnlyList<EventEnvelope>> ReadStreamAsync(
            string streamId, string aggregateType, long fromStreamVersion, int maxCount, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EventEnvelope>>([]);

        public Task<IReadOnlyList<EventEnvelope>> ReadCommittedFeedAsync(
            long afterGlobalPosition, int maxCount, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EventEnvelope>>([]);
    }

    private sealed class FailingSnapshotStoreProvider : ISnapshotStoreProvider
    {
        public int SnapshotFrequency => 1;

        public Task<SnapshotEnvelope?> ReadAsync(
            string streamId,
            string aggregateType,
            long? maxStreamVersion,
            CancellationToken cancellationToken) => Task.FromResult<SnapshotEnvelope?>(null);

        public Task WriteAsync(SnapshotEnvelope snapshot, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Simulated snapshot storage failure.");
    }
}
