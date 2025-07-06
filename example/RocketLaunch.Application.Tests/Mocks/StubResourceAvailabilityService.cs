// RocketLaunch.Domain.Tests/Mocks/StubResourceAvailabilityService.cs

using RocketLaunch.Domain.Service;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Application.Tests.Mocks;

public class StubResourceAvailabilityService : IResourceAvailabilityService
{
    public bool RocketIsAvailable     { get; set; } = true;
    public bool LaunchPadIsAvailable  { get; set; } = true;
    public bool CrewIsAvailable       { get; set; } = true;

    public Task<bool> IsRocketAvailableAsync(RocketId rocketId, LaunchWindow window)
        => Task.FromResult(RocketIsAvailable);

    public Task<bool> IsLaunchPadAvailableAsync(LaunchPadId padId, LaunchWindow window)
        => Task.FromResult(LaunchPadIsAvailable);

    public Task<bool> AreCrewMembersAvailableAsync(IEnumerable<CrewMemberId> crewIds, LaunchWindow window)
        => Task.FromResult(CrewIsAvailable);
}