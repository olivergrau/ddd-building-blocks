namespace RocketLaunch.ReadModel.Core.Model;

public class Rocket
{
    public Guid RocketId { get; set; }
    public string Name { get; set; } = default!;
    
    public double ThrustCapacity { get; set; }
    public int PayloadCapacityKg { get; set; }
    public int CrewCapacity { get; set; }

    public RocketStatus Status { get; set; } = RocketStatus.Unknown;

    public Guid? AssignedMissionId { get; set; }
}

public enum RocketStatus
{
    Unknown,
    Available,
    Assigned,
    UnderMaintenance
}
