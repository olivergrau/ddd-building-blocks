namespace RocketLaunch.Application.Command;

/// <summary>
/// Command to move a mission into the "Scheduled" state.
/// 
/// Preconditions:
/// - Rocket, pad, and required crew must be assigned.
/// 
/// Side Effects:
/// - Emits MissionScheduled domain event.
/// </summary>
public class ScheduleMissionCommand : DDD.BuildingBlocks.Core.Commanding.Command
{
    public Guid MissionId { get; }

    public ScheduleMissionCommand(Guid missionId)
        : base(missionId.ToString(), -1)
    {
        MissionId = missionId;
    }
}