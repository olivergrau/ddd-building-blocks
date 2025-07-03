namespace RocketLaunch.Application.Command;

/// <summary>
/// Command to assign a rocket to a planned mission.
/// 
/// Preconditions:
/// - Rocket must be available and meet payload + crew capacity.
/// 
/// Side Effects:
/// - Emits RocketAssigned domain event.
/// </summary>
public class AssignRocketCommand : DDD.BuildingBlocks.Core.Commanding.Command
{
    public Guid MissionId { get; }
    public Guid RocketId  { get; }

    public AssignRocketCommand(Guid missionId, Guid rocketId)
        : base(missionId.ToString(), -1)
    {
        MissionId = missionId;
        RocketId  = rocketId;
    }
}