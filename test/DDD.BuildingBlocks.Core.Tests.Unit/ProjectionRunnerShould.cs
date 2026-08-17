using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Projection;
using DDD.BuildingBlocks.DevelopmentPackage.Projection;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using Xunit;

namespace DDD.BuildingBlocks.Core.Tests.Unit;

public sealed class ProjectionRunnerShould
{
    [Fact]
    public async Task Process_the_committed_feed_in_position_batches()
    {
        await using var eventStore = new InMemoryEventStoreProvider();
        await using var checkpoints = new InMemoryProjectionCheckpointStore();
        await AppendAsync(eventStore, "stream-1", 0, 3);
        var projection = new RecordingProjection();
        var runner = new ProjectionRunner(eventStore, checkpoints);

        var first = await runner.RunBatchAsync(projection, 2, CancellationToken.None);
        var second = await runner.RunBatchAsync(projection, 2, CancellationToken.None);

        Assert.Equal(2, first.AppliedCount);
        Assert.True(first.HasMore);
        Assert.Equal(1, second.ReadCount);
        Assert.Equal([0L, 1L, 2L], projection.Positions);
    }

    [Fact]
    public async Task Expose_a_poison_event_and_retry_it_without_skipping()
    {
        await using var eventStore = new InMemoryEventStoreProvider();
        await using var checkpoints = new InMemoryProjectionCheckpointStore();
        await AppendAsync(eventStore, "stream-1", 0, 2);
        var projection = new RecordingProjection(failOnceAt: 1);
        var runner = new ProjectionRunner(eventStore, checkpoints);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runner.RunBatchAsync(projection, 10, CancellationToken.None));
        var failed = await checkpoints.GetAsync(projection.Key, CancellationToken.None);

        Assert.Equal(0, failed.LastProcessedPosition);
        Assert.Equal(ProjectionStatus.Faulted, failed.Status);
        Assert.Equal(1, failed.FailedPosition);

        var retry = await runner.RunBatchAsync(projection, 10, CancellationToken.None);
        var recovered = await checkpoints.GetAsync(projection.Key, CancellationToken.None);

        Assert.Equal(1, retry.AppliedCount);
        Assert.Equal(1, recovered.LastProcessedPosition);
        Assert.Equal(ProjectionStatus.Running, recovered.Status);
        Assert.Null(recovered.LastError);
    }

    [Fact]
    public async Task Rebuild_to_the_same_result_as_incremental_processing()
    {
        await using var eventStore = new InMemoryEventStoreProvider();
        await using var checkpoints = new InMemoryProjectionCheckpointStore();
        await AppendAsync(eventStore, "stream-1", 0, 3);
        var projection = new RecordingProjection();
        var runner = new ProjectionRunner(eventStore, checkpoints);
        await runner.RunBatchAsync(projection, 10, CancellationToken.None);
        var incremental = projection.Positions.ToArray();

        await runner.RebuildAsync(
            projection,
            (_, _) => { projection.Positions.Clear(); return Task.CompletedTask; },
            CancellationToken.None);
        var rebuilding = await checkpoints.GetAsync(projection.Key, CancellationToken.None);
        await runner.RunBatchAsync(projection, 10, CancellationToken.None);

        Assert.Equal(ProjectionStatus.Rebuilding, rebuilding.Status);
        Assert.Equal(incremental, projection.Positions);
    }

    [Fact]
    public async Task Keep_independent_projection_checkpoints()
    {
        await using var eventStore = new InMemoryEventStoreProvider();
        await using var checkpoints = new InMemoryProjectionCheckpointStore();
        await AppendAsync(eventStore, "stream-1", 0, 2);
        var first = new RecordingProjection("first");
        var second = new RecordingProjection("second");
        var runner = new ProjectionRunner(eventStore, checkpoints);

        await runner.RunBatchAsync(first, 1, CancellationToken.None);
        await runner.RunBatchAsync(second, 10, CancellationToken.None);

        Assert.Single(first.Positions);
        Assert.Equal(2, second.Positions.Count);
        Assert.Equal(0, (await checkpoints.GetAsync(first.Key, CancellationToken.None)).LastProcessedPosition);
        Assert.Equal(1, (await checkpoints.GetAsync(second.Key, CancellationToken.None)).LastProcessedPosition);
    }

    private static async Task AppendAsync(InMemoryEventStoreProvider store, string streamId, long firstVersion, int count)
    {
        var events = new List<EventEnvelope>(count);
        for (var offset = 0; offset < count; offset++)
        {
            using var payload = JsonDocument.Parse($"{{\"value\":{offset}}}");
            events.Add(new EventEnvelope(
                Guid.NewGuid(), streamId, "order", firstVersion + offset, null, "order.changed", 1,
                DateTimeOffset.UtcNow, null, null, null, null, null, null, payload.RootElement));
        }

        await store.AppendAsync(streamId, "order", firstVersion - 1, events, CancellationToken.None);
    }

    private sealed class RecordingProjection(string name = "orders", long? failOnceAt = null) : IEventProjection
    {
        private long? _failOnceAt = failOnceAt;

        public ProjectionKey Key { get; } = new(name, 1);
        public List<long> Positions { get; } = [];

        public Task ApplyAsync(
            EventEnvelope envelope,
            ProjectionTransactionContext transactionContext,
            CancellationToken cancellationToken)
        {
            var position = envelope.GlobalPosition!.Value;
            if (_failOnceAt == position)
            {
                _failOnceAt = null;
                throw new InvalidOperationException("poison event");
            }

            Positions.Add(position);
            return Task.CompletedTask;
        }
    }
}
