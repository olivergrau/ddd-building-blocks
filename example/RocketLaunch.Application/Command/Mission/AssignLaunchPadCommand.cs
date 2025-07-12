namespace RocketLaunch.Application.Command.Mission;

/// <summary>
/// Command to assign a launch pad to a mission.
/// 
/// Preconditions:
/// - Rocket must already be assigned.
/// - Launch pad must be available for the full launch window.
/// 
/// Side Effects:
/// - Emits LaunchPadAssigned domain event.
/// </summary>
public class AssignLaunchPadCommand(Guid missionId, Guid launchPadId)
    : DDD.BuildingBlocks.Core.Commanding.Command(missionId.ToString(), -1)
{
    public Guid MissionId   { get; } = missionId;
    public Guid LaunchPadId { get; } = launchPadId;
}