namespace RocketLaunch.Application.Command.Mission;

/// <summary>
/// Command to move a mission into the "Scheduled" state.
/// 
/// Preconditions:
/// - Rocket, pad, and required crew must be assigned.
/// 
/// Side Effects:
/// - Emits MissionScheduled domain event.
/// </summary>
public class ScheduleMissionCommand(Guid missionId)
    : DDD.BuildingBlocks.Core.Commanding.Command(missionId.ToString(), -1)
{
    public Guid MissionId { get; } = missionId;
}