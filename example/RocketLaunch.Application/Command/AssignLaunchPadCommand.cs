namespace RocketLaunch.Application.Command;

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
public class AssignLaunchPadCommand : DDD.BuildingBlocks.Core.Commanding.Command
{
    public Guid MissionId   { get; }
    public Guid LaunchPadId { get; }

    public AssignLaunchPadCommand(Guid missionId, Guid launchPadId)
        : base(missionId.ToString(), -1)
    {
        MissionId   = missionId;
        LaunchPadId = launchPadId;
    }
}