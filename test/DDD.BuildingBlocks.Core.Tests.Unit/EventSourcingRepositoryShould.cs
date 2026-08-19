using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Exception;
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
    [Fact(DisplayName = "Persist an explicitly registered stable aggregate type key")]
    [Trait("Category", "Unittest")]
    public async Task Persist_an_explicitly_registered_stable_aggregate_type_key()
    {
        const string aggregateType = "orders.order";
        var eventStore = new InMemoryEventStoreProvider();
        var repository = EventSourcingRepository.Create(
            eventStore,
            DDD.BuildingBlocks.Tests.Abstracts.Event.TestEventCodec.Create(),
            new AggregateTypeRegistry().Register<Order>(aggregateType));
        var aggregate = new Order(Guid.NewGuid().ToString(), "Title", "Comment", OrderState.Open);

        await repository.SaveAsync(aggregate, CancellationToken.None);

        var stored = await eventStore.ReadCommittedFeedAsync(-1, 10, CancellationToken.None);
        stored.Should().ContainSingle().Which.AggregateType.Should().Be(aggregateType);
        var reloaded = await repository.GetByIdAsync<Order, OrderId>(aggregate.Id, CancellationToken.None);
        reloaded.Should().NotBeNull();
    }

    [Fact(DisplayName = "Persist explicit commit metadata in every event envelope")]
    [Trait("Category", "Unittest")]
    public async Task Persist_explicit_commit_metadata_in_every_event_envelope()
    {
        var eventStore = new InMemoryEventStoreProvider();
        var repository = EventSourcingRepository.Create(
            eventStore,
            DDD.BuildingBlocks.Tests.Abstracts.Event.TestEventCodec.Create(),
            new AggregateTypeRegistry().Register<Order>("orders.order"));
        var aggregate = new Order(Guid.NewGuid().ToString(), "Title", "Comment", OrderState.Open);
        var metadata = new EventCommitMetadata(
            "correlation-17",
            "causation-16",
            "command-15",
            "human:owner",
            "turn-14");

        await repository.SaveAsync(aggregate, metadata, CancellationToken.None);

        var stored = (await eventStore.ReadCommittedFeedAsync(-1, 10, CancellationToken.None)).Should().ContainSingle().Which;
        stored.CorrelationId.Should().Be(metadata.CorrelationId);
        stored.CausationId.Should().Be(metadata.CausationId);
        stored.CommandId.Should().Be(metadata.CommandId);
        stored.Actor.Should().Be(metadata.Actor);
        stored.TurnId.Should().Be(metadata.TurnId);
    }

    [Fact(DisplayName = "Reject duplicate and missing aggregate type registrations")]
    [Trait("Category", "Unittest")]
    public void Reject_duplicate_and_missing_aggregate_type_registrations()
    {
        var registry = new AggregateTypeRegistry().Register<Order>("orders.order");

        var duplicateType = () => registry.Register<Order>("orders.renamed");
        var duplicateKey = () => registry.Register(typeof(string), "orders.order");
        var missing = () => new AggregateTypeRegistry().GetAggregateType(typeof(Order));

        duplicateType.Should().Throw<AggregateTypeRegistrationException>();
        duplicateKey.Should().Throw<AggregateTypeRegistrationException>();
        missing.Should().Throw<UnknownAggregateTypeException>();
    }

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
