using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;
using DDD.BuildingBlocks.Core.Persistence.Storage;
using Xunit;

namespace DDD.BuildingBlocks.Tests.Abstracts;

public abstract class SnapshotStoreProviderContract
{
    protected abstract ISnapshotStoreProvider CreateProvider(int frequency = 10);

    [Fact]
    public async Task Expose_the_configured_frequency()
    {
        await WithProvider(async provider =>
        {
            Assert.Equal(7, provider.SnapshotFrequency);
            await Task.CompletedTask;
        }, 7);
    }

    [Fact]
    public async Task Store_and_read_the_latest_snapshot()
    {
        await WithProvider(async provider =>
        {
            await provider.WriteAsync(CreateEnvelope("stream", "order", 3), CancellationToken.None);
            await provider.WriteAsync(CreateEnvelope("stream", "order", 7), CancellationToken.None);

            var snapshot = await provider.ReadAsync("stream", "order", null, CancellationToken.None);

            Assert.NotNull(snapshot);
            Assert.Equal(7, snapshot.StreamVersion);
        });
    }

    [Fact]
    public async Task Read_the_latest_snapshot_not_newer_than_a_version()
    {
        await WithProvider(async provider =>
        {
            await provider.WriteAsync(CreateEnvelope("stream", "order", 3), CancellationToken.None);
            await provider.WriteAsync(CreateEnvelope("stream", "order", 7), CancellationToken.None);

            var snapshot = await provider.ReadAsync("stream", "order", 5, CancellationToken.None);

            Assert.NotNull(snapshot);
            Assert.Equal(3, snapshot.StreamVersion);
        });
    }

    [Fact]
    public async Task Distinguish_aggregate_types_with_the_same_stream_id()
    {
        await WithProvider(async provider =>
        {
            await provider.WriteAsync(CreateEnvelope("shared", "order", 1), CancellationToken.None);
            await provider.WriteAsync(CreateEnvelope("shared", "invoice", 2), CancellationToken.None);

            Assert.Equal(1, (await provider.ReadAsync("shared", "order", null, CancellationToken.None))!.StreamVersion);
            Assert.Equal(2, (await provider.ReadAsync("shared", "invoice", null, CancellationToken.None))!.StreamVersion);
        });
    }

    [Fact]
    public async Task Replace_the_same_snapshot_version_idempotently()
    {
        await WithProvider(async provider =>
        {
            await provider.WriteAsync(CreateEnvelope("stream", "order", 1, 1), CancellationToken.None);
            await provider.WriteAsync(CreateEnvelope("stream", "order", 1, 2), CancellationToken.None);

            var snapshot = await provider.ReadAsync("stream", "order", null, CancellationToken.None);

            Assert.Equal(2, snapshot!.Payload.GetProperty("value").GetInt32());
        });
    }

    [Fact]
    public async Task Honor_cancellation_without_wrapping_it()
    {
        await WithProvider(async provider =>
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                provider.ReadAsync("stream", "order", null, cancellation.Token));
        });
    }

    private async Task WithProvider(Func<ISnapshotStoreProvider, Task> test, int frequency = 10)
    {
        var provider = CreateProvider(frequency);
        try
        {
            await test(provider);
        }
        finally
        {
            if (provider is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync();
            else if (provider is IDisposable disposable) disposable.Dispose();
        }
    }

    private static SnapshotEnvelope CreateEnvelope(
        string streamId,
        string aggregateType,
        long version,
        int value = 1)
    {
        using var payload = JsonDocument.Parse($"{{\"value\":{value}}}");
        return new SnapshotEnvelope(
            streamId, aggregateType, version, "test.snapshot", 1, DateTimeOffset.UtcNow, payload.RootElement);
    }
}
