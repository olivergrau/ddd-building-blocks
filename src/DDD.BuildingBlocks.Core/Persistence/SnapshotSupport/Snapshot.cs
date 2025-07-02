namespace DDD.BuildingBlocks.Core.Persistence.SnapshotSupport
{
    using System;

    [Serializable]
    public abstract class Snapshot
    {
        public string AggregateTypeIdentifier { get; }
        public string SerializedAggregateId { get; }
        public int Version { get; }

        protected Snapshot(string serializedAggregateId, int version, string aggregateTypeIdentifier)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(version);

            SerializedAggregateId = serializedAggregateId ?? throw new ArgumentNullException(nameof(serializedAggregateId));
            Version = version;
            AggregateTypeIdentifier = aggregateTypeIdentifier ?? throw new ArgumentNullException(nameof(aggregateTypeIdentifier));
        }
    }
}
