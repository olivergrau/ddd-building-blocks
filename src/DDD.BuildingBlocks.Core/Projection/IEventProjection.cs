using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Core.Projection;

public interface IEventProjection
{
    ProjectionKey Key { get; }

    Task ApplyAsync(
        EventEnvelope envelope,
        ProjectionTransactionContext transactionContext,
        CancellationToken cancellationToken);
}
