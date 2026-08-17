namespace DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;

public interface ISnapshotCodec
{
    SnapshotEnvelope Encode(Snapshot snapshot, string aggregateType);
    Snapshot? Decode(SnapshotEnvelope envelope);
}
