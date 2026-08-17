using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.ErrorHandling;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Persistence.Storage;
using Xunit;

namespace DDD.BuildingBlocks.Tests.Abstracts;

public abstract class EventStoreProviderContract
{
    protected abstract IEventStoreProvider CreateProvider();

    [Fact]
    public async Task Append_and_read_a_new_stream()
    {
        await WithProvider(async provider =>
        {
            var events = CreateEvents("stream-1", "order", 0, 2);

            var result = await provider.AppendAsync("stream-1", "order", -1, events, CancellationToken.None);
            var stored = await provider.ReadStreamAsync("stream-1", "order", 0, 10, CancellationToken.None);

            Assert.Equal(1, result.CurrentStreamVersion);
            Assert.Equal([0L, 1L], stored.Select(item => item.StreamVersion));
            Assert.All(stored, item => Assert.NotNull(item.CommittedAt));
        });
    }

    [Fact]
    public async Task Read_a_stream_from_a_version_with_a_limit()
    {
        await WithProvider(async provider =>
        {
            await provider.AppendAsync(
                "stream-1",
                "order",
                -1,
                CreateEvents("stream-1", "order", 0, 4),
                CancellationToken.None);

            var stored = await provider.ReadStreamAsync("stream-1", "order", 2, 1, CancellationToken.None);

            Assert.Single(stored);
            Assert.Equal(2, stored[0].StreamVersion);
        });
    }

    [Fact]
    public async Task Distinguish_streams_by_aggregate_type_and_stream_id()
    {
        await WithProvider(async provider =>
        {
            await provider.AppendAsync(
                "shared-id", "order", -1, CreateEvents("shared-id", "order", 0, 1), CancellationToken.None);
            await provider.AppendAsync(
                "shared-id", "invoice", -1, CreateEvents("shared-id", "invoice", 0, 1), CancellationToken.None);

            var order = await provider.ReadStreamAsync("shared-id", "order", 0, 10, CancellationToken.None);
            var invoice = await provider.ReadStreamAsync("shared-id", "invoice", 0, 10, CancellationToken.None);

            Assert.Single(order);
            Assert.Single(invoice);
            Assert.NotEqual(order[0].EventId, invoice[0].EventId);
        });
    }

    [Fact]
    public async Task Reject_an_incorrect_expected_version_atomically()
    {
        await WithProvider(async provider =>
        {
            await provider.AppendAsync(
                "stream-1", "order", -1, CreateEvents("stream-1", "order", 0, 1), CancellationToken.None);

            var exception = await Assert.ThrowsAsync<EventStoreConcurrencyException>(() => provider.AppendAsync(
                "stream-1", "order", 5, CreateEvents("stream-1", "order", 6, 2), CancellationToken.None));
            var stream = await provider.ReadStreamAsync("stream-1", "order", 0, 10, CancellationToken.None);
            var feed = await provider.ReadCommittedFeedAsync(-1, 10, CancellationToken.None);

            Assert.Equal(ErrorClassification.ConcurrencyConflict, exception.ErrorInfo.Classification);
            Assert.Single(stream);
            Assert.Single(feed);
        });
    }

    [Fact]
    public async Task Allow_only_one_parallel_writer_for_the_same_expected_version()
    {
        await WithProvider(async provider =>
        {
            var first = provider.AppendAsync(
                "stream-1", "order", -1, CreateEvents("stream-1", "order", 0, 1), CancellationToken.None);
            var second = provider.AppendAsync(
                "stream-1", "order", -1, CreateEvents("stream-1", "order", 0, 1), CancellationToken.None);

            var outcomes = await Task.WhenAll(Observe(first), Observe(second));
            var stored = await provider.ReadStreamAsync("stream-1", "order", 0, 10, CancellationToken.None);

            Assert.Single(outcomes, outcome => outcome is null);
            var conflict = Assert.Single(outcomes, outcome => outcome is EventStreamAlreadyExistsException);
            Assert.Equal(
                ErrorClassification.StreamAlreadyExists,
                ((EventStreamAlreadyExistsException)conflict!).ErrorInfo.Classification);
            Assert.Single(stored);
        });
    }

    [Fact]
    public async Task Classify_an_append_to_a_missing_stream()
    {
        await WithProvider(async provider =>
        {
            var exception = await Assert.ThrowsAsync<EventStreamNotFoundException>(() => provider.AppendAsync(
                "missing", "order", 0, CreateEvents("missing", "order", 1, 1), CancellationToken.None));

            Assert.Equal(ErrorClassification.StreamNotFound, exception.ErrorInfo.Classification);
        });
    }

    [Fact]
    public async Task Reject_a_duplicate_event_id_without_partial_commit()
    {
        await WithProvider(async provider =>
        {
            var eventId = Guid.NewGuid();
            await provider.AppendAsync(
                "stream-1", "order", -1, [CreateEnvelope(eventId, "stream-1", "order", 0)], CancellationToken.None);

            var exception = await Assert.ThrowsAsync<DuplicateEventIdException>(() => provider.AppendAsync(
                "stream-2", "order", -1, [CreateEnvelope(eventId, "stream-2", "order", 0)], CancellationToken.None));
            var secondStream = await provider.ReadStreamAsync("stream-2", "order", 0, 10, CancellationToken.None);
            var feed = await provider.ReadCommittedFeedAsync(-1, 10, CancellationToken.None);

            Assert.Equal(ErrorClassification.PermanentProviderFailure, exception.ErrorInfo.Classification);
            Assert.Empty(secondStream);
            Assert.Single(feed);
        });
    }

    [Fact]
    public async Task Preserve_global_commit_order_and_page_the_feed_exclusively()
    {
        await WithProvider(async provider =>
        {
            await provider.AppendAsync(
                "stream-1", "order", -1, CreateEvents("stream-1", "order", 0, 2), CancellationToken.None);
            await provider.AppendAsync(
                "stream-2", "order", -1, CreateEvents("stream-2", "order", 0, 1), CancellationToken.None);

            var firstPage = await provider.ReadCommittedFeedAsync(-1, 2, CancellationToken.None);
            var secondPage = await provider.ReadCommittedFeedAsync(firstPage[^1].GlobalPosition!.Value, 2, CancellationToken.None);

            Assert.Equal([0L, 1L], firstPage.Select(item => item.GlobalPosition!.Value));
            Assert.Single(secondPage);
            Assert.Equal(2, secondPage[0].GlobalPosition);
        });
    }

    [Fact]
    public async Task Retain_an_immutable_payload_after_the_source_document_is_disposed()
    {
        await WithProvider(async provider =>
        {
            EventEnvelope envelope;
            using (var document = JsonDocument.Parse("{\"value\":42}"))
            {
                envelope = CreateEnvelope(Guid.NewGuid(), "stream-1", "order", 0, document.RootElement);
            }

            await provider.AppendAsync("stream-1", "order", -1, [envelope], CancellationToken.None);
            var stored = await provider.ReadStreamAsync("stream-1", "order", 0, 1, CancellationToken.None);

            Assert.Equal(42, stored[0].Payload.GetProperty("value").GetInt32());
        });
    }

    [Fact]
    public async Task Honor_cancellation_without_wrapping_it()
    {
        await WithProvider(async provider =>
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.AppendAsync(
                "stream-1", "order", -1, CreateEvents("stream-1", "order", 0, 1), cancellation.Token));
        });
    }

    private async Task WithProvider(Func<IEventStoreProvider, Task> test)
    {
        var provider = CreateProvider();
        try
        {
            await test(provider);
        }
        finally
        {
            if (provider is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private static async Task<System.Exception?> Observe(Task<AppendEventsResult> append)
    {
        try
        {
            await append;
            return null;
        }
        catch (System.Exception exception)
        {
            return exception;
        }
    }

    private static IReadOnlyCollection<EventEnvelope> CreateEvents(
        string streamId,
        string aggregateType,
        long firstVersion,
        int count)
    {
        return Enumerable.Range(0, count)
            .Select(offset => CreateEnvelope(Guid.NewGuid(), streamId, aggregateType, firstVersion + offset))
            .ToArray();
    }

    private static EventEnvelope CreateEnvelope(
        Guid eventId,
        string streamId,
        string aggregateType,
        long streamVersion)
    {
        using var document = JsonDocument.Parse("{\"value\":1}");
        return CreateEnvelope(eventId, streamId, aggregateType, streamVersion, document.RootElement);
    }

    private static EventEnvelope CreateEnvelope(
        Guid eventId,
        string streamId,
        string aggregateType,
        long streamVersion,
        JsonElement payload)
    {
        return new EventEnvelope(
            eventId,
            streamId,
            aggregateType,
            streamVersion,
            null,
            "test-event",
            1,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null,
            null,
            payload);
    }
}
