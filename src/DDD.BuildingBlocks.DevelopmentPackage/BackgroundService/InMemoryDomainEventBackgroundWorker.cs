using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Hosting.Background.Worker;

namespace DDD.BuildingBlocks.DevelopmentPackage.BackgroundService
{
    using System;
    using EventPublishing;

    public class InMemoryDomainEventBackgroundWorker : SingletonBackgroundServiceWorker
    {
        private static readonly object Lock = new();
        private static bool _isRunning;

        private readonly IDomainEventHandler _eventHandler;
        private readonly EventPublishingTable _eventPublishingTable;
        private readonly string _boundToWorkerId;

        private readonly ILogger _log;

        public InMemoryDomainEventBackgroundWorker(
            IDomainEventHandler eventHandler, EventPublishingTable eventPublishingTable, string boundToWorkerId, ILoggerFactory? loggerFactory = null) : base(loggerFactory)
        {
            if (string.IsNullOrWhiteSpace(boundToWorkerId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(boundToWorkerId));
            }

            _eventHandler = eventHandler;
            _eventPublishingTable = eventPublishingTable;
            _boundToWorkerId = boundToWorkerId;

            var nullLoggerFactory = new NullLoggerFactory();

            _log = loggerFactory != null ? loggerFactory.CreateLogger(nameof(InMemoryDomainEventBackgroundWorker))
                : nullLoggerFactory.CreateLogger(nameof(InMemoryDomainEventBackgroundWorker));

            nullLoggerFactory.Dispose();
        }

        protected override bool IsRunning()
        {
            return _isRunning;
        }

        protected override void SetIsRunning(bool to)
        {
            _isRunning = to;
        }

        protected override object GetLock()
        {
            return Lock;
        }

        protected override async Task WorkAsync(CancellationToken? cancellationToken = null)
        {
            while (_eventPublishingTable.Dequeue(_boundToWorkerId) is { } domainEvent)
            {
                await _eventHandler.HandleAsync(domainEvent);
            }
        }
    }
}
