using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.DI.Extensions.Dispatching;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DDD.BuildingBlocks.Core.Tests.Unit;

public sealed class ScopedCommandDispatcherShould
{
    [Fact]
    public async Task Reject_a_missing_handler()
    {
        await using var provider = CreateServices().BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ICommandDispatcher>();

        var action = () => dispatcher.DispatchAsync(new TestCommand(), CancellationToken.None);

        await action.Should().ThrowAsync<HandlerRegistrationException>();
    }

    [Fact]
    public async Task Reject_duplicate_handlers()
    {
        var services = CreateServices();
        services.AddScoped<ScopeMarker>();
        services.AddScoped<ICommandHandler<TestCommand>, RecordingHandler>();
        services.AddScoped<ICommandHandler<TestCommand>, SecondHandler>();
        await using var provider = services.BuildServiceProvider();

        var action = () => provider.GetRequiredService<ICommandDispatcher>()
            .DispatchAsync(new TestCommand(), CancellationToken.None);

        await action.Should().ThrowAsync<HandlerRegistrationException>();
    }

    [Fact]
    public async Task Preserve_cancellation_without_wrapping_it()
    {
        var services = CreateServices();
        services.AddScoped<ICommandHandler<TestCommand>, RecordingHandler>();
        await using var provider = services.BuildServiceProvider();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = () => provider.GetRequiredService<ICommandDispatcher>()
            .DispatchAsync(new TestCommand(), cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Preserve_cancellation_during_handler_io()
    {
        var services = CreateServices();
        services.AddScoped<ICommandHandler<BlockingCommand>, BlockingHandler>();
        await using var provider = services.BuildServiceProvider();
        using var cancellation = new CancellationTokenSource();

        var dispatch = provider.GetRequiredService<ICommandDispatcher>()
            .DispatchAsync(new BlockingCommand(), cancellation.Token);
        await BlockingHandler.Started.Task;
        cancellation.Cancel();

        Func<Task> action = async () => await dispatch;
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Create_an_independent_scope_for_each_parallel_dispatch()
    {
        RecordingHandler.ScopeIds.Clear();
        var services = CreateServices();
        services.AddScoped<ScopeMarker>();
        services.AddScoped<ICommandHandler<TestCommand>, RecordingHandler>();
        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ICommandDispatcher>();

        await Task.WhenAll(Enumerable.Range(0, 16)
            .Select(_ => dispatcher.DispatchAsync(new TestCommand(), CancellationToken.None)));

        RecordingHandler.ScopeIds.Should().HaveCount(16).And.OnlyHaveUniqueItems();
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICommandDispatcher, ScopedCommandDispatcher>();
        return services;
    }

    private sealed class TestCommand() : Command("test-id", -1);

    private sealed class BlockingCommand() : Command("blocking-id", -1);

    private sealed class ScopeMarker
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed class RecordingHandler(ScopeMarker marker) : ICommandHandler<TestCommand>
    {
        public static ConcurrentBag<Guid> ScopeIds { get; } = [];

        public Task HandleAsync(TestCommand command, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ScopeIds.Add(marker.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class SecondHandler : ICommandHandler<TestCommand>
    {
        public Task HandleAsync(TestCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class BlockingHandler : ICommandHandler<BlockingCommand>
    {
        public static TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task HandleAsync(BlockingCommand command, CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }
}
