using DDD.BuildingBlocks.Core.Exception;
using LunarOps.Domain.Model;
using LunarOps.SharedKernel.Enums;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.Domain.Service
{
    public sealed class PayloadHandling
    {
        public async Task Unload(LunarMission mission, MoonStation station, IStationAvailabilityService stationAvailabilityService)
        {
            ArgumentNullException.ThrowIfNull(mission);
            ArgumentNullException.ThrowIfNull(station);

            if (mission.Status != LunarMissionStatus.Docked && mission.Status != LunarMissionStatus.CrewTransferred)
                throw new AggregateValidationException(mission.Id, nameof(mission.Status), mission.Status, "Mission must be docked before unloading payload.");

            var totalMass = mission.PayloadManifest.Sum(p => p.Mass);
            var hasCapacity = await stationAvailabilityService.HasStorageCapacityAsync(station.Id, totalMass);
            if (!hasCapacity)
                throw new RuleValidationException(mission.Id, "MoonStation does not have enough storage capacity.");

            foreach (var (item, mass) in mission.PayloadManifest)
            {
                var payload = new LunarPayload(item, mass, "Unknown"); // DestinationArea should be stored in manifest if needed
                station.StorePayload(payload);
            }

            mission.UnloadPayload(); // raises PayloadUnloaded
        }
    }
}