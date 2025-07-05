namespace RocketLaunch.ReadModel.Core.Model;

public class Rocket
{
    public Guid RocketId { get; set; }
    public string RocketName { get; set; } = default!;
    public string VehicleType { get; set; } = default!; // e.g., Falcon 9, Starship

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
