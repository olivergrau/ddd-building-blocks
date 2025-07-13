using RocketLaunch.Domain.Service;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.ReadModel.InMemory.Service
{
    public class InMemoryStationAvailabilityService(
        IRocketService rocketService,
        ILaunchPadService padService,
        ICrewMemberService crewService)
        : IResourceAvailabilityService
    {
        public Task<bool> IsRocketAvailableAsync(RocketId rocketId, LaunchWindow window)
        {
            var rocket = rocketService.GetById(rocketId.Value);
            return Task.FromResult(rocket is null or { Status: RocketStatus.Available });
        }

        public Task<bool> IsLaunchPadAvailableAsync(LaunchPadId padId, LaunchWindow window)
        {
            var pad = padService.GetById(padId.Value);
            
            if (pad == null || pad.Status == LaunchPadStatus.Available)
                return Task.FromResult(true);
            
            if (pad.Status == LaunchPadStatus.UnderMaintenance)
                return Task.FromResult(false);

            var overlaps = pad.OccupiedWindows.Any(w =>
                w.Start < window.End && window.Start < w.End);

            return Task.FromResult(!overlaps);
        }

        public Task<bool> AreCrewMembersAvailableAsync(IEnumerable<CrewMemberId> crewIds, LaunchWindow window)
        {
            return Task.FromResult(
                !crewIds.Select(id => crewService.GetById(id.Value)).Any(member => member is not { Status: CrewMemberStatus.Available }));
        }
    }
}