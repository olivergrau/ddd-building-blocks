using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Hosting.Background.Queue
{
	public sealed class BlockingBackgroundTaskQueue : IBackgroundTaskQueue, IDisposable
    {
		private readonly ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new();

		private readonly SemaphoreSlim _signal = new(0);

		public void QueueBackgroundWorkItem(
			Func<CancellationToken, Task> workItem)
		{
			if (workItem == null)
			{
				throw new ArgumentNullException(nameof(workItem));
			}

			_workItems.Enqueue(workItem);
			_signal.Release();
		}

		public async Task<Func<CancellationToken, Task>> DequeueAsync(
			CancellationToken cancellationToken)
		{
			await _signal.WaitAsync(cancellationToken);
			_workItems.TryDequeue(out var workItem);

			return workItem ?? throw new InvalidOperationException();
		}


        public void Dispose() => _signal.Dispose();
    }
}
