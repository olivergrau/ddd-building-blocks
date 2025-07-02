using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DDD.BuildingBlocks.Hosting.Background.Constants;
using DDD.BuildingBlocks.Hosting.Background.Worker;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace DDD.BuildingBlocks.Hosting.Background
{
    using Core;

    public sealed class TimedHostedService<T>(T worker, ILoggerFactory loggerFactory, IOptions<TimedHostedServiceOptions>? options = null)
        : TimedHostedService(worker, loggerFactory, options)
        where T : IBackgroundServiceWorker;

	public class TimedHostedService : BackgroundService
	{
		private readonly ILogger _logger;
		private readonly IBackgroundServiceWorker _worker;

        private readonly int _triggerEveryMilliseconds = 10;

        public event EventHandler<ErrorEventArgs>? OnError;

        protected virtual void OnErrorRaised(ErrorEventArgs args)
        {
            OnError?.Invoke(this, args);
        }

		public TimedHostedService(IBackgroundServiceWorker worker, ILoggerFactory loggerFactory, IOptions<TimedHostedServiceOptions>? options = null)
		{
			_worker = worker;
			_logger = loggerFactory.CreateLogger<TimedHostedService>();

			if (options != null && options.Value.GlobalTriggerTimeoutInMilliseconds > 0)
			{
				_triggerEveryMilliseconds = options.Value.GlobalTriggerTimeoutInMilliseconds;
				_logger.LogWarning(HostingBackgroundLogMessages.GlobalTriggerTimeoutApplied, options.Value.GlobalTriggerTimeoutInMilliseconds, GetType());
			}

			if (options == null || options.Value.SpecificTriggerTimeoutsInMilliseconds.Count <= 0)
			{
				return;
			}

			foreach (var triggerValues in options.Value.SpecificTriggerTimeoutsInMilliseconds)
			{
				_logger.LogTrace(HostingBackgroundLogMessages.InspectingTypeWithValue, triggerValues.Key, triggerValues.Value);
			}

			if (!options.Value.SpecificTriggerTimeoutsInMilliseconds.ContainsKey(GetType()))
			{
				return;
			}

			var timeout = options.Value.SpecificTriggerTimeoutsInMilliseconds[GetType()];

			if (timeout <= 0)
			{
				return;
			}

			_triggerEveryMilliseconds = timeout;
			_logger.LogWarning(HostingBackgroundLogMessages.LocalTriggerTimeoutApplied, timeout, GetType());
		}

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(HostingBackgroundLogMessages.TimedBackgroundServiceStarting);
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_triggerEveryMilliseconds));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ExecuteWorker(stoppingToken);
            }
        }

        private async Task ExecuteWorker(CancellationToken cancellationToken)
        {
            try
            {
                await _worker.ProcessAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                OnErrorRaised(new ErrorEventArgs(ex, $"BackgroundWorker-Error: {ex.Message}"));

                _logger.LogError(HostingBackgroundLogMessages.ErrorExecutingWorker, ex);
            }
        }
    }
}
