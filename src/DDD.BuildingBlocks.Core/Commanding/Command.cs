using DDD.BuildingBlocks.Core.Exception;

namespace DDD.BuildingBlocks.Core.Commanding
{
    public abstract class Command : ICommand
    {
        public AggregateSourcingMode Mode { get; set; }

        public string? CorrelationId { get; }
        public string? SerializedAggregateId { get; }

        public int TargetVersion { get; }

        protected Command(string? serializedAggregateId, int targetVersion)
        {
            if (string.IsNullOrWhiteSpace(serializedAggregateId))
            {
                throw new CommandCreationFailedException($"Value cannot be null or whitespace: {nameof(serializedAggregateId)}");
            }

            CorrelationId = CorrelatedScope.Current;

            SerializedAggregateId = serializedAggregateId;

            if (targetVersion < -1)
            {
                throw new CommandCreationFailedException("TargetVersion must not be less than -1.");
            }

            TargetVersion = targetVersion;
            Mode = AggregateSourcingMode.Update;
        }
    }
}
