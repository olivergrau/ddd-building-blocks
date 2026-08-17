using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Projection;
using Xunit;

namespace DDD.BuildingBlocks.Tests.Abstracts;

public abstract class ProjectionCheckpointStoreContract
{
    protected abstract IProjectionCheckpointStore CreateStore();

    [Fact]
    public async Task Start_at_the_initial_checkpoint()
    {
        await WithStore(async store =>
        {
            var checkpoint = await store.GetAsync(new ProjectionKey("orders", 1), CancellationToken.None);

            Assert.Equal(-1, checkpoint.LastProcessedPosition);
            Assert.Equal(ProjectionStatus.Idle, checkpoint.Status);
        });
    }

    [Fact]
    public async Task Apply_and_checkpoint_an_event_atomically()
    {
        await WithStore(async store =>
        {
            var applied = false;
            var key = new ProjectionKey("orders", 1);

            var processed = await store.ProcessAsync(
                key, CreateEnvelope(0), (_, _) => { applied = true; return Task.CompletedTask; }, CancellationToken.None);
            var checkpoint = await store.GetAsync(key, CancellationToken.None);

            Assert.True(processed);
            Assert.True(applied);
            Assert.Equal(0, checkpoint.LastProcessedPosition);
            Assert.Equal(ProjectionStatus.Running, checkpoint.Status);
        });
    }

    [Fact]
    public async Task Ignore_an_already_checkpointed_redelivery()
    {
        await WithStore(async store =>
        {
            var key = new ProjectionKey("orders", 1);
            var applyCount = 0;
            Func<ProjectionTransactionContext, CancellationToken, Task> apply = (_, _) =>
            {
                applyCount++;
                return Task.CompletedTask;
            };

            await store.ProcessAsync(key, CreateEnvelope(0), apply, CancellationToken.None);
            var processed = await store.ProcessAsync(key, CreateEnvelope(0), apply, CancellationToken.None);

            Assert.False(processed);
            Assert.Equal(1, applyCount);
        });
    }

    [Fact]
    public async Task Keep_the_checkpoint_when_application_fails()
    {
        await WithStore(async store =>
        {
            var key = new ProjectionKey("orders", 1);

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.ProcessAsync(
                key,
                CreateEnvelope(0),
                (_, _) => throw new InvalidOperationException("poison"),
                CancellationToken.None));
            var checkpoint = await store.GetAsync(key, CancellationToken.None);

            Assert.Equal(-1, checkpoint.LastProcessedPosition);
        });
    }

    [Fact]
    public async Task Expose_a_failed_projection_without_advancing_it()
    {
        await WithStore(async store =>
        {
            var key = new ProjectionKey("orders", 1);

            await store.MarkFailedAsync(key, 0, "poison event", CancellationToken.None);
            var checkpoint = await store.GetAsync(key, CancellationToken.None);

            Assert.Equal(-1, checkpoint.LastProcessedPosition);
            Assert.Equal(ProjectionStatus.Faulted, checkpoint.Status);
            Assert.Equal(0, checkpoint.FailedPosition);
            Assert.Contains("poison", checkpoint.LastError, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Reject_a_gap_in_the_global_feed()
    {
        await WithStore(async store =>
        {
            var key = new ProjectionKey("orders", 1);

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.ProcessAsync(
                key, CreateEnvelope(1), (_, _) => Task.CompletedTask, CancellationToken.None));
        });
    }

    [Fact]
    public async Task Reset_the_read_model_and_checkpoint_for_rebuild()
    {
        await WithStore(async store =>
        {
            var key = new ProjectionKey("orders", 1);
            await store.ProcessAsync(key, CreateEnvelope(0), (_, _) => Task.CompletedTask, CancellationToken.None);
            var reset = false;

            await store.ResetAsync(
                key, (_, _) => { reset = true; return Task.CompletedTask; }, CancellationToken.None);
            var checkpoint = await store.GetAsync(key, CancellationToken.None);

            Assert.True(reset);
            Assert.Equal(-1, checkpoint.LastProcessedPosition);
            Assert.Equal(ProjectionStatus.Rebuilding, checkpoint.Status);
        });
    }

    private async Task WithStore(Func<IProjectionCheckpointStore, Task> test)
    {
        var store = CreateStore();
        try
        {
            await test(store);
        }
        finally
        {
            if (store is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (store is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private static EventEnvelope CreateEnvelope(long position)
    {
        using var payload = JsonDocument.Parse("{\"value\":1}");
        return new EventEnvelope(
            Guid.NewGuid(), "stream", "order", position, position, "order.changed", 1,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, null, null, null, payload.RootElement);
    }
}
