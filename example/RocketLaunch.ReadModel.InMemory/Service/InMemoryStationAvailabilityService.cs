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
            return rocketService.IsAvailableAsync(rocketId.Value);
        }

        public Task<bool> IsLaunchPadAvailableAsync(LaunchPadId padId, LaunchWindow window)
        {
            return padService.IsAvailableAsync(padId.Value, window.Start, window.End);
        }

        public async Task<bool> AreCrewMembersAvailableAsync(IEnumerable<CrewMemberId> crewIds, LaunchWindow window)
        {
            var members = await Task.WhenAll(crewIds.Select(id => crewService.GetByIdAsync(id.Value))).ConfigureAwait(false);
            return members.All(member => member is { Status: CrewMemberStatus.Available });
        }
    }
}