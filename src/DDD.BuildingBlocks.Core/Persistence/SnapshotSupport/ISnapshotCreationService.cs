namespace DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;

using System.Threading.Tasks;

public interface ISnapshotCreationService
{
    Task<Snapshot?> CreateSnapshotFrom(string aggregateId, long version, System.Threading.CancellationToken cancellationToken);
}
