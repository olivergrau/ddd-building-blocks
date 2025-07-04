using DDD.BuildingBlocks.Core.Exception;
using LunarOps.Domain.Model;
using LunarOps.SharedKernel.Enums;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.Domain.Service
{
    public sealed class UndockingManeuver
    {
        public void Undock(LunarMission mission, MoonStation station)
        {
            if (mission.Status != LunarMissionStatus.InService)
                throw new AggregateValidationException(
                    mission.Id, nameof(mission.Status), mission.Status,
                    "Only in-service missions can initiate undocking."
                );

            if (mission.AssignedPort is null)
                throw new AggregateValidationException(
                    mission.Id, nameof(mission.AssignedPort), null,
                    "No docking port assigned to this mission."
                );

            // Release docking port at MoonStation
            station.ReleaseDockingPort(mission.AssignedPort);

            // Mark mission as departed
            mission.Depart();
        }
    }
}