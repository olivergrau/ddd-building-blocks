using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Core.Persistence.Storage
{
    using SnapshotSupport;

    public interface ISnapshotStorageProvider
    {
        int SnapshotFrequency { get; }
        
        Task<Snapshot?> GetSnapshotAsync(string aggregateId);
        
        Task<Snapshot?> GetSnapshotAsync(string aggregateId, int version);
        
        Task SaveSnapshotAsync(Snapshot snapshot);
    }
}
