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
            return Task.FromResult(
                rocketService.IsAvailable(rocketId.Value));
        }

        public Task<bool> IsLaunchPadAvailableAsync(LaunchPadId padId, LaunchWindow window)
        {
            return Task.FromResult(
                padService.IsAvailable(padId.Value, window.Start, window.End));
        }

        public Task<bool> AreCrewMembersAvailableAsync(IEnumerable<CrewMemberId> crewIds, LaunchWindow window)
        {
            return Task.FromResult(
                !crewIds.Select(id => crewService.GetById(id.Value)).Any(member => member is not { Status: CrewMemberStatus.Available }));
        }
    }
}