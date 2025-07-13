using DDD.BuildingBlocks.Core.Commanding;

namespace RocketLaunch.Application.Command.Mission;

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
    
    public string      Name            { get; }
    public double      ThrustCapacity  { get; }
    public int      PayloadCapacityKg { get; }
    public int         CrewCapacity    { get; }
    
    public AssignRocketCommand(
        Guid missionId,
        Guid rocketId,
        string name,
        double thrustCapacity,
        int payloadCapacityKg,
        int crewCapacity)
        : base(missionId.ToString(), -1)
    {
        MissionId = missionId;
        RocketId  = rocketId;
        Name            = name;
        ThrustCapacity  = thrustCapacity;
        PayloadCapacityKg = payloadCapacityKg;
        CrewCapacity    = crewCapacity;

        Mode = AggregateSourcingMode.Update;
    }
}