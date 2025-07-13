using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RocketLaunch.Api.Tests;

public class ApiErrorTests : IClassFixture<RocketLaunchApiFactory>
{
    private readonly HttpClient _client;

    public ApiErrorTests(RocketLaunchApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private record ErrorPayload(string Message, string Origin, string Classification);

    [Fact]
    public async Task GetMission_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/missions/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ScheduleMission_WithoutResources_ReturnsUnprocessableEntity()
    {
        var missionId = Guid.NewGuid();
        var mission = new
        {
            MissionId = missionId,
            MissionName = "Test",
            TargetOrbit = "LEO",
            PayloadDescription = "Sat",
            LaunchWindowStart = DateTime.UtcNow,
            LaunchWindowEnd = DateTime.UtcNow.AddHours(1)
        };
        var reg = await _client.PostAsJsonAsync("/missions/", mission);
        reg.EnsureSuccessStatusCode();

        var schedule = await _client.PostAsync($"/missions/{missionId}/schedule", null);
        Assert.Equal((HttpStatusCode)422, schedule.StatusCode);

        var payload = await schedule.Content.ReadFromJsonAsync<ErrorPayload>();
        Assert.NotNull(payload);
        Assert.Equal("ProcessingError", payload!.Classification);
    }

    [Fact]
    public async Task GetRocket_InvalidId_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/entities/rockets/00000000-0000-0000-0000-000000000000");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>();
        Assert.NotNull(payload);
        Assert.Equal("InputDataError", payload!.Classification);
    }
}
