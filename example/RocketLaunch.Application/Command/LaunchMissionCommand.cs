namespace RocketLaunch.Application.Command;

/// <summary>
/// Command to mark a mission as successfully launched.
/// 
/// Preconditions:
/// - Mission must be in "Scheduled" state.
/// 
/// Side Effects:
/// - Emits MissionLaunched domain event.
/// </summary>
public class LaunchMissionCommand : DDD.BuildingBlocks.Core.Commanding.Command
{
    public Guid MissionId { get; }

    public LaunchMissionCommand(Guid missionId)
        : base(missionId.ToString(), -1)
    {
        MissionId = missionId;
    }
}