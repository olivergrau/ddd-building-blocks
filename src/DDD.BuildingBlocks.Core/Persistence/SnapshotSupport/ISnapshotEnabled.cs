namespace DDD.BuildingBlocks.Core.Persistence.SnapshotSupport
{
    public interface ISnapshotEnabled
    {
        Snapshot? TakeSnapshot();
        void ApplySnapshot(Snapshot snapshot);
    }
}
