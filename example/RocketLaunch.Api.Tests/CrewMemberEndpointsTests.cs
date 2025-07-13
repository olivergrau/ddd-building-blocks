using System.Net.Http.Json;
using RocketLaunch.Api;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;
using RocketLaunch.SharedKernel.Enums;
using ReadModelCrewStatus = RocketLaunch.ReadModel.Core.Model.CrewMemberStatus;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace RocketLaunch.Api.Tests;

public class CrewMemberEndpointsTests : IClassFixture<RocketLaunchApiFactory>
{
    private readonly RocketLaunchApiFactory _factory;
    private readonly HttpClient _client;

    public CrewMemberEndpointsTests(RocketLaunchApiFactory factory)
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
    public async Task CrewMemberLifecycle_FullCycle()
    {
        var crewId = Guid.NewGuid();

        // register
        var register = new
        {
            CrewMemberId = crewId,
            Name = "Alice",
            Role = "Commander",
            Certifications = new[] { "L1" }
        };
        var regResp = await _client.PostAsJsonAsync("/crew-members/", register);
        regResp.EnsureSuccessStatusCode();
        var crewService = _factory.Services.GetRequiredService<ICrewMemberService>();
        await WaitAsync(async () => await crewService.GetByIdAsync(crewId) != null);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());

        // verify created
        var crew = await _client.GetFromJsonAsync<CrewMember>($"/crew-members/{crewId}", options);
        Assert.NotNull(crew);
        Assert.Equal(ReadModelCrewStatus.Available, crew!.Status);
        Assert.Single(crew.CertificationLevels);

        // update certifications
        var certs = new { Certifications = new[] { "L1", "L2" } };
        var certResp = await _client.PostAsJsonAsync($"/crew-members/{crewId}/certifications", certs);
        certResp.EnsureSuccessStatusCode();
        await WaitAsync(async () => (await crewService.GetByIdAsync(crewId))!.CertificationLevels.Count == 2);

        // set status unavailable then available
        var status = new { Status = "Unavailable" };
        var statusResp = await _client.PostAsJsonAsync($"/crew-members/{crewId}/status", status);
        statusResp.EnsureSuccessStatusCode();
        await WaitAsync(async () => (await crewService.GetByIdAsync(crewId))!.Status == ReadModelCrewStatus.Unavailable);

        status = new { Status = "Available" };
        statusResp = await _client.PostAsJsonAsync($"/crew-members/{crewId}/status", status);
        statusResp.EnsureSuccessStatusCode();
        await WaitAsync(async () => (await crewService.GetByIdAsync(crewId))!.Status == ReadModelCrewStatus.Available);

        // assign
        var assignResp = await _client.PostAsync($"/crew-members/{crewId}/assign", null);
        assignResp.EnsureSuccessStatusCode();
        await WaitAsync(async () => (await crewService.GetByIdAsync(crewId))!.Status == ReadModelCrewStatus.Assigned);

        // release
        var releaseResp = await _client.PostAsync($"/crew-members/{crewId}/release", null);
        releaseResp.EnsureSuccessStatusCode();
        await WaitAsync(async () => (await crewService.GetByIdAsync(crewId))!.Status == ReadModelCrewStatus.Available);

        crew = await _client.GetFromJsonAsync<CrewMember>($"/crew-members/{crewId}", options);
        Assert.NotNull(crew);
        Assert.Equal(ReadModelCrewStatus.Available, crew!.Status);
        Assert.Equal(2, crew.CertificationLevels.Count);
    }

    [Fact]
    public async Task GetAll_ReturnsRegisteredMembers()
    {
        var crewId1 = Guid.NewGuid();
        var crewId2 = Guid.NewGuid();

        var register1 = new { CrewMemberId = crewId1, Name = "Bob", Role = "FlightEngineer", Certifications = Array.Empty<string>() };
        var register2 = new { CrewMemberId = crewId2, Name = "Carol", Role = "Pilot", Certifications = Array.Empty<string>() };

        var resp1 = await _client.PostAsJsonAsync("/crew-members/", register1);
        resp1.EnsureSuccessStatusCode();
        var resp2 = await _client.PostAsJsonAsync("/crew-members/", register2);
        resp2.EnsureSuccessStatusCode();

        var crewService = _factory.Services.GetRequiredService<ICrewMemberService>();
        await WaitAsync(async () => (await crewService.GetAllAsync()).Count() >= 2);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());

        var all = await _client.GetFromJsonAsync<List<CrewMember>>("/crew-members/", options);
        Assert.NotNull(all);
        Assert.Contains(all!, c => c.CrewMemberId == crewId1);
        Assert.Contains(all!, c => c.CrewMemberId == crewId2);
    }
}
