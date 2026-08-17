using System.Threading;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;

namespace DDD.BuildingBlocks.Core.Persistence.Storage;

public interface ISnapshotStoreProvider
{
    int SnapshotFrequency { get; }

    Task<SnapshotEnvelope?> ReadAsync(
        string streamId,
        string aggregateType,
        long? maxStreamVersion,
        CancellationToken cancellationToken);

    Task WriteAsync(SnapshotEnvelope snapshot, CancellationToken cancellationToken);
}
