

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace DDD.BuildingBlocks.DevelopmentPackage.EventHandling
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Core.Commanding;
    using Core.Event;
    using Core.Util;

    public class BlackHoleEventHandler : IDomainEventHandler
    {
        private readonly ILogger _log;

        public BlackHoleEventHandler(ILoggerFactory? loggerFactory = null)
        {
            var nullLoggerFactory = new NullLoggerFactory();

            _log = loggerFactory != null ? loggerFactory.CreateLogger(nameof(DefaultCommandProcessor))
                : nullLoggerFactory.CreateLogger(nameof(DomainEventNotifier));

            nullLoggerFactory.Dispose();
        }

        public Task HandleAsync(IDomainEvent @event)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            _log.LogInformation(
                $"DomainEvent #{@event.TargetVersion + 1} on aggregate {@event.SerializedAggregateId} absorbed in black hole: {@event.GetType().Name} @ {ApplicationTime.Current.ToLongTimeString()}");

            return Task.CompletedTask;
        }
    }
}
