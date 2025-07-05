using DDD.BuildingBlocks.Core.Exception;
using LunarOps.Domain.Model;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.Domain.Service
{
    public class DockingScheduler
    {
        private readonly IStationAvailabilityService _stationValidator;

        public DockingScheduler(IStationAvailabilityService stationValidator)
        {
            _stationValidator = stationValidator;
        }

        public async Task ScheduleDockingAsync(
            LunarMission mission,
            MoonStation station)
        {
            // Validate consistency between relation and provided station
            if (mission.StationRelation.AggregateId != station.Id.Value.ToString())
                throw new InvalidOperationException("Mission is not assigned to this station.");

            // Check real-time availability
            if (!await _stationValidator.HasFreePortAsync(station.Id))
                throw new RuleValidationException(mission.Id, "Station has no available docking port");

            var crewCount = mission.CrewManifest.Count;
            var totalPayload = mission.PayloadManifest.Sum(p => p.Mass);

            if (!await _stationValidator.HasCrewCapacityAsync(station.Id, crewCount))
                throw new RuleValidationException(mission.Id, "Station has no remaining crew capacity");

            if (!await _stationValidator.HasStorageCapacityAsync(station.Id, totalPayload))
                throw new RuleValidationException(mission.Id, "Station has no remaining payload capacity");

            // Mutate both aggregates (within coordination boundary)
            var reservedPort = station.ReserveDockingPort(mission.Id, mission.VehicleType ?? throw new InvalidOperationException());
            mission.AssignDockingPort(reservedPort);
        }
    }
}