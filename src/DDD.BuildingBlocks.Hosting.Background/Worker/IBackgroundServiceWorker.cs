using System.Threading;
using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Hosting.Background.Worker
{
	public interface IBackgroundServiceWorker
	{
		Task ProcessAsync(CancellationToken? cancellationToken = null);
	}
}