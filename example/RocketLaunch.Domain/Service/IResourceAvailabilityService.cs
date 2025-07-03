// Domain/Services/IResourceAvailabilityService.cs

using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Domain.Service
{
    public interface IResourceAvailabilityService
    {
        Task<bool> IsRocketAvailableAsync(RocketId rocketId, LaunchWindow window);
        Task<bool> IsLaunchPadAvailableAsync(LaunchPadId padId, LaunchWindow window);
        Task<bool> AreCrewMembersAvailableAsync(
            IEnumerable<CrewMemberId> crewIds, LaunchWindow window);
    }
}
