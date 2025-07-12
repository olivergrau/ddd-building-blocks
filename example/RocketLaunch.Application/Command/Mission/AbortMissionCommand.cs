namespace RocketLaunch.Application.Command.Mission;

/// <summary>
/// Command to abort a mission before launch.
/// 
/// Preconditions:
/// - Mission must not be in Launched state.
/// 
/// Side Effects:
/// - Emits MissionAborted domain event.
/// </summary>
public class AbortMissionCommand(Guid missionId) : DDD.BuildingBlocks.Core.Commanding.Command(missionId.ToString(), -1)
{
    public Guid MissionId { get; } = missionId;
}