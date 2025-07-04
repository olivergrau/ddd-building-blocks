using DDD.BuildingBlocks.Core.Exception;
using LunarOps.Domain.Model;
using LunarOps.Domain.Model.Entities;
using LunarOps.SharedKernel.Enums;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.Domain.Service
{
    public sealed class CrewMemberTransfer
    {
        public async Task TransferCrew(LunarMission mission, MoonStation station, IStationAvailabilityService stationAvailabilityService)
        {
            ArgumentNullException.ThrowIfNull(mission);
            ArgumentNullException.ThrowIfNull(station);

            if (mission.Status != LunarMissionStatus.Docked && mission.Status != LunarMissionStatus.PayloadUnloaded)
                throw new AggregateValidationException(mission.Id, nameof(mission.Status), mission.Status, "Mission must be docked before transferring crew.");

            var crewCount = mission.CrewManifest.Count;
            var hasCapacity = await stationAvailabilityService.HasCrewCapacityAsync(station.Id, crewCount);
            if (!hasCapacity)
                throw new RuleValidationException(mission.Id, "Station does not have sufficient crew capacity.");

            foreach (var (name, role) in mission.CrewManifest)
            {
                var crewId = new LunarCrewMemberId(Guid.NewGuid()); // or derive from name
                var member = new LunarCrewMember(crewId, name, role);
                station.AssignCrewMember(member);
            }

            var ids = station.CrewQuarters.Select(c => c.Id).ToList(); // assumes all new ones were added
            mission.TransferCrew(ids);
        }
    }
}