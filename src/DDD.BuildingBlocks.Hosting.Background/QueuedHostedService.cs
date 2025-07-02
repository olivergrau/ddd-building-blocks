using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DDD.BuildingBlocks.Hosting.Background.Constants;
using DDD.BuildingBlocks.Hosting.Background.Queue;
// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace DDD.BuildingBlocks.Hosting.Background
{
    using System.Globalization;

    /// <summary>
    ///     Service which monitors a queue and executes work items
    ///     in the sequence the work items are enqueued.
    /// </summary>
    public class QueuedHostedService(IBackgroundTaskQueue taskQueue, ILoggerFactory loggerFactory) : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<QueuedHostedService>();

        public IBackgroundTaskQueue TaskQueue { get; } = taskQueue;

        protected override async Task ExecuteAsync(
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(HostingBackgroundLogMessages.QueuedHostingServiceStarting);

            while (!cancellationToken.IsCancellationRequested)
            {
                // blocks if queue is empty
                var workItem = await TaskQueue.DequeueAsync(cancellationToken);

                try
                {
                    await workItem(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        string.Format(CultureInfo.InvariantCulture, HostingBackgroundLogMessages.ErrorOccuredExecuting, nameof(workItem)));
                }
            }

            _logger.LogInformation(HostingBackgroundLogMessages.QueuedHostingServiceStopping);
        }
    }
}
