using System.Net.Http.Json;
using RocketLaunch.Api;
using RocketLaunch.Application.Dto;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.SharedKernel.Enums;
using ReadModelRocketStatus = RocketLaunch.ReadModel.Core.Model.RocketStatus;
using ReadModelLaunchPadStatus = RocketLaunch.ReadModel.Core.Model.LaunchPadStatus;
using RocketLaunch.ReadModel.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace RocketLaunch.Api.Tests;

public class MissionEndpointsTests : IClassFixture<RocketLaunchApiFactory>
{
    private readonly RocketLaunchApiFactory _factory;
    private readonly HttpClient _client;

    public MissionEndpointsTests(RocketLaunchApiFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    private static async Task WaitAsync(Func<Task<bool>> condition, int timeout = 2000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeout)
        {
            if (await condition()) return;
            await Task.Delay(50);
        }
    }

    [Fact]
    public async Task MissionLifecycle_FullCycle()
    {
        var missionId = Guid.NewGuid();
        var rocketId = Guid.NewGuid();
        var padId = Guid.NewGuid();
        var crewId = Guid.NewGuid();
        
        // register crew member
        var crewRegister = new
        {
            CrewMemberId = crewId,
            Name = "Alice",
            Role = "Commander",
            Certifications = new[] { "L1" }
        };
        var crewResponse = await _client.PostAsJsonAsync("/crew-members/", crewRegister);
        crewResponse.EnsureSuccessStatusCode();
        var crewService = _factory.Services.GetRequiredService<ICrewMemberService>();
        await WaitAsync(async () => await crewService.GetByIdAsync(crewId) != null);

        // register mission
        var registerMission = new
        {
            MissionId = missionId,
            MissionName = "Apollo",
            TargetOrbit = "Moon",
            PayloadDescription = "Rover",
            LaunchWindowStart = DateTime.UtcNow,
            LaunchWindowEnd = DateTime.UtcNow.AddHours(2)
        };
        var regResponse = await _client.PostAsJsonAsync("/missions/", registerMission);
        regResponse.EnsureSuccessStatusCode();
        var missionService2 = _factory.Services.GetRequiredService<IMissionService>();
        await WaitAsync(async () => await missionService2.GetByIdAsync(missionId) != null);
        var missionService = _factory.Services.GetRequiredService<IMissionService>();
        await WaitAsync(async () => await missionService.GetByIdAsync(missionId) != null);

        // assign rocket
        var assignRocket = new
        {
            RocketId = rocketId,
            RocketName = "Falcon 9",
            ThrustCapacity = 7600d,
            PayloadCapacityKg = 22800,
            CrewCapacity = 7
        };
        var rocketResp = await _client.PostAsJsonAsync($"/missions/{missionId}/assign-rocket", assignRocket);
        rocketResp.EnsureSuccessStatusCode();
        await Task.Delay(100);

        // assign pad
        var assignPad = new
        {
            LaunchPadId = padId,
            LaunchPadName = "Pad 39A",
            LaunchPadLocation = "Cape",
            SupportedRockets = new[] { "Falcon 9" }
        };
        var padResp = await _client.PostAsJsonAsync($"/missions/{missionId}/assign-pad", assignPad);
        padResp.EnsureSuccessStatusCode();
        await Task.Delay(100);

        // assign crew
        var assignCrew = new { CrewMemberIds = new[] { crewId } };
        var crewAssignResp = await _client.PostAsJsonAsync($"/missions/{missionId}/assign-crew", assignCrew);
        crewAssignResp.EnsureSuccessStatusCode();
        await Task.Delay(1000);

        // schedule
        var scheduleResp = await _client.PostAsync($"/missions/{missionId}/schedule", null);
        scheduleResp.EnsureSuccessStatusCode();
        await Task.Delay(1000);

        // launch
        var launchResp = await _client.PostAsync($"/missions/{missionId}/launch", null);
        launchResp.EnsureSuccessStatusCode();
        await Task.Delay(1000);

        // arrive
        var arrive = new
        {
            ArrivalTime = DateTime.UtcNow,
            VehicleType = "Starship",
            CrewManifest = Array.Empty<CrewManifestItemDto>(),
            PayloadManifest = Array.Empty<PayloadManifestItemDto>()
        };
        var arriveResp = await _client.PostAsJsonAsync($"/missions/{missionId}/arrive", arrive);
        arriveResp.EnsureSuccessStatusCode();

        // wait a bit for projections
        await Task.Delay(1000);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        
        var mission = await _client.GetFromJsonAsync<Mission>($"/missions/{missionId}", options);
        
        Assert.NotNull(mission);
        Assert.Equal(MissionStatus.Arrived, mission!.Status);
        Assert.Equal(rocketId, mission.AssignedRocketId);
        Assert.Equal(padId, mission.AssignedPadId);
        Assert.Contains(crewId, mission.CrewMemberIds);
    }

    [Fact]
    public async Task MissionAbort_ReleasesResources()
    {
        var missionId = Guid.NewGuid();
        var rocketId = Guid.NewGuid();
        var padId = Guid.NewGuid();

        // register mission
        var registerMission = new
        {
            MissionId = missionId,
            MissionName = "Test",
            TargetOrbit = "LEO",
            PayloadDescription = "Sat",
            LaunchWindowStart = DateTime.UtcNow,
            LaunchWindowEnd = DateTime.UtcNow.AddHours(1)
        };
        var regResponse = await _client.PostAsJsonAsync("/missions/", registerMission);
        regResponse.EnsureSuccessStatusCode();

        // assign rocket
        var assignRocket = new
        {
            RocketId = rocketId,
            RocketName = "Falcon",
            ThrustCapacity = 7600d,
            PayloadCapacityKg = 22800,
            CrewCapacity = 7
        };
        var rocketResp = await _client.PostAsJsonAsync($"/missions/{missionId}/assign-rocket", assignRocket);
        rocketResp.EnsureSuccessStatusCode();

        // assign pad
        var assignPad = new
        {
            LaunchPadId = padId,
            LaunchPadName = "Pad",
            LaunchPadLocation = "Cape",
            SupportedRockets = new[] { "Falcon" }
        };
        var padResp = await _client.PostAsJsonAsync($"/missions/{missionId}/assign-pad", assignPad);
        padResp.EnsureSuccessStatusCode();

        // schedule
        var scheduleResp = await _client.PostAsync($"/missions/{missionId}/schedule", null);
        scheduleResp.EnsureSuccessStatusCode();

        // abort
        var abortResp = await _client.PostAsync($"/missions/{missionId}/abort", null);
        abortResp.EnsureSuccessStatusCode();
        await Task.Delay(1000);
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        
        var mission = await _client.GetFromJsonAsync<Mission>($"/missions/{missionId}", options);
        Assert.NotNull(mission);
        Assert.Equal(MissionStatus.Aborted, mission!.Status);

        var rocket = await _client.GetFromJsonAsync<Rocket>($"/entities/rockets/{rocketId}", options);
        Assert.NotNull(rocket);
        Assert.Equal(ReadModelRocketStatus.Available, rocket!.Status);

        var pad = await _client.GetFromJsonAsync<LaunchPad>($"/entities/launchpads/{padId}", options);
        Assert.NotNull(pad);
        Assert.Equal(ReadModelLaunchPadStatus.Available, pad!.Status);
    }
}

