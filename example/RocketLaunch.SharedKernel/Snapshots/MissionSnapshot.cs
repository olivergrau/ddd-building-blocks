using DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;
using RocketLaunch.SharedKernel.Enums;

namespace RocketLaunch.SharedKernel.Snapshots;

[Serializable]
public class MissionSnapshot : Snapshot
{
    public MissionSnapshot(string serializedAggregateId, int version)
        : base(serializedAggregateId, version, "Mission")
    {
    }
    
    public Guid Id { get; init; }
    public int CurrentVersion { get; init; }
    public int LastCommittedVersion { get; init; }

    public required string Name { get; init; }
    public required string TargetOrbit { get; init; }
    public required string Payload { get; init; }
    
    public DateTime LaunchWindowStart { get; init; }
    public DateTime LaunchWindowEnd { get; init; }
    
    public MissionStatus Status { get; init; }

    public Guid? AssignedRocketId { get; init; }
    public Guid? AssignedPadId { get; init; }

    public List<Guid> CrewMemberIds { get; init; } = new();
}
