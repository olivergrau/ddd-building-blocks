using System.Threading;
using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Core.Persistence.Storage
{
    using SnapshotSupport;

    public interface ISnapshotStorageProvider
    {
        int SnapshotFrequency { get; }
        
        Task<Snapshot?> GetSnapshotAsync(string aggregateId, CancellationToken cancellationToken);
        
        Task<Snapshot?> GetSnapshotAsync(string aggregateId, long version, CancellationToken cancellationToken);
        
        Task SaveSnapshotAsync(Snapshot snapshot, CancellationToken cancellationToken);
    }
}
