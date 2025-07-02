using System.Threading;
using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Hosting.Background.Worker
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    /// <summary>
	///     Helper class that ensures that the ProcessAsync method can only be called once per App Domain.
	/// </summary>
	public abstract class SingletonBackgroundServiceWorker : IBackgroundServiceWorker
	{
        private readonly ILogger _logger;

        protected SingletonBackgroundServiceWorker(ILoggerFactory? loggerFactory)
        {
            _logger = loggerFactory != null ? loggerFactory.CreateLogger(GetType().Name) : new NullLogger<SingletonBackgroundServiceWorker>();
        }

		protected abstract bool IsRunning();

		protected abstract void SetIsRunning(bool to);

		protected abstract object GetLock();

		public virtual Task ProcessAsync(CancellationToken? cancellationToken = null)
		{
			lock (GetLock())
			{
				if (IsRunning())
				{
					_logger.LogDebug("SingletonBackgroundWorker {TypeName} already running", GetType().Name);
					return Task.CompletedTask;
				}
				_logger.LogDebug("SingletonBackgroundWorker {TypeName} set isRunning to true", GetType().Name);
				SetIsRunning(true);


			    try
			    {
				    _logger.LogDebug("SingletonBackgroundWorker {TypeName} is doing some work", GetType().Name);
				    return WorkAsync(cancellationToken);
			    }
			    finally
			    {
				    _logger.LogDebug("SingletonBackgroundWorker {TypeName} set free", GetType().Name);
				    SetIsRunning(false);
			    }
            }
		}

		protected abstract Task WorkAsync(CancellationToken? cancellationToken = null);
	}
}
