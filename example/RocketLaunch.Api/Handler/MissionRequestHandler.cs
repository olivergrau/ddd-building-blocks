using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RocketLaunch.Application;
using RocketLaunch.Application.Command.Mission;
using RocketLaunch.Application.Dto;
using RocketLaunch.ReadModel.Core.Service;

namespace RocketLaunch.Api.Handler;

internal static class MissionRequestHandler
{
    internal static void MapMissionRoutes(this WebApplication app)
    {
        var group = app.MapGroup("missions")
            .WithTags("Missions");
        
        group.MapPost("/", async ([FromServices] IDomainEntry entry, [FromBody] RegisterMissionRequest request) =>
        {
            var cmd = new RegisterMissionCommand(request.MissionId, request.MissionName, request.TargetOrbit, request.PayloadDescription, new LaunchWindowDto(request.LaunchWindowStart, request.LaunchWindowEnd));
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        group.MapPost("/{missionId:guid}/assign-rocket", async ([FromServices] IDomainEntry entry, [FromRoute] Guid missionId, [FromBody] AssignRocketRequest request) =>
        {
            var cmd = new AssignRocketCommand(missionId, request.RocketId, request.RocketName, request.ThrustCapacity, request.PayloadCapacityKg, request.CrewCapacity);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        group.MapPost("/{missionId:guid}/assign-pad", async ([FromServices] IDomainEntry entry, [FromRoute] Guid missionId, [FromBody] AssignLaunchPadRequest request) =>
        {
            var cmd = new AssignLaunchPadCommand(missionId, request.LaunchPadId, request.LaunchPadName, request.LaunchPadLocation, request.SupportedRockets);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        group.MapPost("/{missionId:guid}/assign-crew", async ([FromServices] IDomainEntry entry, [FromRoute] Guid missionId, [FromBody] AssignCrewRequest request) =>
        {
            var cmd = new AssignCrewCommand(missionId, request.CrewMemberIds);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        group.MapPost("/{missionId:guid}/schedule", async ([FromServices] IDomainEntry entry, [FromRoute] Guid missionId) =>
        {
            var cmd = new ScheduleMissionCommand(missionId);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        group.MapPost("/{missionId:guid}/launch", async ([FromServices] IDomainEntry entry, [FromRoute] Guid missionId) =>
        {
            var cmd = new LaunchMissionCommand(missionId);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        group.MapPost("/{missionId:guid}/abort", async ([FromServices] IDomainEntry entry, [FromRoute] Guid missionId) =>
        {
            var cmd = new AbortMissionCommand(missionId);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        group.MapPost("/{missionId:guid}/arrive", async ([FromServices] IDomainEntry entry, [FromRoute] Guid missionId, [FromBody] MarkMissionArrivedRequest request) =>
        {
            var cmd = new MarkMissionArrivedCommand(missionId, request.ArrivalTime, request.VehicleType, request.CrewManifest, request.PayloadManifest);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        group.MapGet("/{missionId:guid}", ([FromServices] IMissionService service, [FromRoute] Guid missionId) =>
        {
            var mission = service.GetById(missionId);
            return mission is null ? Results.NotFound() : Results.Ok(mission);
        });

        group.MapGet("/", ([FromServices] IMissionService service) => Results.Ok(service.GetAll()));

        group.MapGet("/rockets/{id:guid}", ([FromServices] IRocketService service, [FromRoute] Guid id) =>
        {
            var rocket = service.GetById(id);
            return rocket is null ? Results.NotFound() : Results.Ok(rocket);
        });
    }
}

internal record RegisterMissionRequest(Guid MissionId, string MissionName, string TargetOrbit, string PayloadDescription, DateTime LaunchWindowStart, DateTime LaunchWindowEnd);
internal record AssignRocketRequest(Guid RocketId, string RocketName, double ThrustCapacity, int PayloadCapacityKg, int CrewCapacity);
internal record AssignLaunchPadRequest(Guid LaunchPadId, string LaunchPadName, string LaunchPadLocation, string[] SupportedRockets);
internal record AssignCrewRequest([property: Required] IReadOnlyCollection<Guid> CrewMemberIds);
internal record MarkMissionArrivedRequest(DateTime ArrivalTime, string VehicleType, IReadOnlyCollection<CrewManifestItemDto> CrewManifest, IReadOnlyCollection<PayloadManifestItemDto> PayloadManifest);
