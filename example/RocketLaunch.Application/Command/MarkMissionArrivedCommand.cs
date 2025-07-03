using RocketLaunch.Application.Dto;

namespace RocketLaunch.Application.Command;

/// <summary>
/// Command to mark a mission as "Arrived" at lunar orbit.
/// 
/// Preconditions:
/// - Mission must be in "Launched" state.
/// 
/// Side Effects:
/// - Emits MissionArrivedAtLunarOrbit integration event.
/// - Finalizes mission state.
/// </summary>
public class MarkMissionArrivedCommand : DDD.BuildingBlocks.Core.Commanding.Command
{
    public Guid MissionId { get; }
    public DateTime ArrivalTime { get; }
    public string VehicleType { get; }
    public IReadOnlyCollection<CrewManifestItemDto> CrewManifest { get; }
    public IReadOnlyCollection<PayloadManifestItemDto> PayloadManifest { get; }

    public MarkMissionArrivedCommand(
        Guid missionId,
        DateTime arrivalTime,
        string vehicleType,
        IEnumerable<CrewManifestItemDto> crewManifest,
        IEnumerable<PayloadManifestItemDto> payloadManifest
    ) : base(missionId.ToString(), -1)
    {
        MissionId       = missionId;
        ArrivalTime     = arrivalTime;
        VehicleType     = vehicleType ?? throw new ArgumentNullException(nameof(vehicleType));
        CrewManifest    = new List<CrewManifestItemDto>(crewManifest ?? throw new ArgumentNullException(nameof(crewManifest))).AsReadOnly();
        PayloadManifest = new List<PayloadManifestItemDto>(payloadManifest ?? throw new ArgumentNullException(nameof(payloadManifest))).AsReadOnly();
    }
}