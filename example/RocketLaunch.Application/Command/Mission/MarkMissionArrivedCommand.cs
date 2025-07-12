using RocketLaunch.Application.Dto;

namespace RocketLaunch.Application.Command.Mission;

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
public class MarkMissionArrivedCommand(
    Guid missionId,
    DateTime arrivalTime,
    string vehicleType,
    IEnumerable<CrewManifestItemDto> crewManifest,
    IEnumerable<PayloadManifestItemDto> payloadManifest)
    : DDD.BuildingBlocks.Core.Commanding.Command(missionId.ToString(), -1)
{
    public Guid MissionId { get; } = missionId;
    public DateTime ArrivalTime { get; } = arrivalTime;
    public string VehicleType { get; } = vehicleType ?? throw new ArgumentNullException(nameof(vehicleType));
    public IReadOnlyCollection<CrewManifestItemDto> CrewManifest { get; } = new List<CrewManifestItemDto>(crewManifest ?? throw new ArgumentNullException(nameof(crewManifest))).AsReadOnly();
    public IReadOnlyCollection<PayloadManifestItemDto> PayloadManifest { get; } = new List<PayloadManifestItemDto>(payloadManifest ?? throw new ArgumentNullException(nameof(payloadManifest))).AsReadOnly();
}