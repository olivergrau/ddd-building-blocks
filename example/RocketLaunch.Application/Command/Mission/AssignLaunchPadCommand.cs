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
public class AssignLaunchPadCommand(Guid missionId, Guid launchPadId, string name, string location, string[] supportedRockets)
    : DDD.BuildingBlocks.Core.Commanding.Command(missionId.ToString(), -1)
{
    public Guid MissionId   { get; } = missionId;
    public Guid LaunchPadId { get; } = launchPadId;
    
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    public string Location { get; } = location ?? throw new ArgumentNullException(nameof(location));
    public string[] SupportedRockets { get; } = supportedRockets ?? throw new ArgumentNullException(nameof(supportedRockets));
}