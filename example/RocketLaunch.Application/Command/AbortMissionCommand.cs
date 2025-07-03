namespace RocketLaunch.Application.Command;

/// <summary>
/// Command to abort a mission before launch.
/// 
/// Preconditions:
/// - Mission must not be in Launched state.
/// 
/// Side Effects:
/// - Emits MissionAborted domain event.
/// </summary>
public class AbortMissionCommand : DDD.BuildingBlocks.Core.Commanding.Command
{
    public Guid MissionId { get; }

    public AbortMissionCommand(Guid missionId)
        : base(missionId.ToString(), -1)
    {
        MissionId = missionId;
    }
}