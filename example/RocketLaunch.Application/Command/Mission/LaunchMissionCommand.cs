namespace RocketLaunch.Application.Command.Mission;

/// <summary>
/// Command to mark a mission as successfully launched.
/// 
/// Preconditions:
/// - Mission must be in "Scheduled" state.
/// 
/// Side Effects:
/// - Emits MissionLaunched domain event.
/// </summary>
public class LaunchMissionCommand(Guid missionId) : DDD.BuildingBlocks.Core.Commanding.Command(missionId.ToString(), -1)
{
    public Guid MissionId { get; } = missionId;
}