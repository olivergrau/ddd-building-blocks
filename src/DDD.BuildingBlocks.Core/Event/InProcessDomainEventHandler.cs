using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DDD.BuildingBlocks.Core.Util;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace DDD.BuildingBlocks.Core.Event
{
    /// <summary>
    ///     This implementation executes events in the context of the executing process and notifies subscribers.
    /// </summary>
    public class InProcessDomainEventHandler : IDomainEventHandler
    {
        private readonly IDomainEventNotifier _domainEventNotifier;
        private readonly ILogger _log;

        public InProcessDomainEventHandler(IDomainEventNotifier domainEventNotifier, ILoggerFactory? loggerFactory = null)
        {
            _domainEventNotifier = domainEventNotifier ?? throw new ArgumentNullException(nameof(domainEventNotifier));

            var nullLoggerFactory = new NullLoggerFactory();

            _log = loggerFactory != null ? loggerFactory.CreateLogger(nameof(InProcessDomainEventHandler))
                : nullLoggerFactory.CreateLogger(nameof(InProcessDomainEventHandler));

            nullLoggerFactory.Dispose();
        }

        public async Task HandleAsync(IDomainEvent @event)
        {
            await _domainEventNotifier.NotifyAsync(@event);

            _log.LogInformation(
                $"DomainEvent #{@event.TargetVersion + 1} handled: {@event.GetType().Name} @ {ApplicationTime.Current.ToLongTimeString()}");
        }
    }
}
