using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using RocketLaunch.Application;
using RocketLaunch.Application.Command.CrewMember;
using RocketLaunch.ReadModel.Core.Service;
using RocketLaunch.SharedKernel.Enums;

namespace RocketLaunch.Api.Handler;

internal static class CrewMemberRequestHandler
{
    internal static void MapCrewMemberRoutes(this WebApplication app)
    {
        var group = app.MapGroup("crew-members")
                        .WithTags("Crew-Members");
        
        group.MapPost("/", async ([FromServices] IDomainEntry entry, [FromBody] RegisterCrewMemberRequest request) =>
        {
            var cmd = new RegisterCrewMemberCommand(request.CrewMemberId, request.Name, request.Role, request.Certifications);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        group.MapPost("/{crewMemberId:guid}/assign", async ([FromServices] IDomainEntry entry, [FromRoute] Guid crewMemberId) =>
        {
            var cmd = new AssignCrewMemberCommand(crewMemberId);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        group.MapPost("/{crewMemberId:guid}/release", async ([FromServices] IDomainEntry entry, [FromRoute] Guid crewMemberId) =>
        {
            var cmd = new ReleaseCrewMemberCommand(crewMemberId);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        group.MapPost("/{crewMemberId:guid}/certifications", async ([FromServices] IDomainEntry entry, [FromRoute] Guid crewMemberId, [FromBody] SetCrewMemberCertificationsRequest request) =>
        {
            var cmd = new SetCrewMemberCertificationsCommand(crewMemberId, request.Certifications);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        group.MapPost("/{crewMemberId:guid}/status", async ([FromServices] IDomainEntry entry, [FromRoute] Guid crewMemberId, [FromBody] SetCrewMemberStatusRequest request) =>
        {
            var cmd = new SetCrewMemberStatusCommand(crewMemberId, request.Status);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        group.MapGet("/{crewMemberId:guid}", ([FromServices] ICrewMemberService service, [FromRoute] Guid crewMemberId) =>
        {
            var crew = service.GetById(crewMemberId);
            return crew is null ? Results.NotFound() : Results.Ok(crew);
        });

        group.MapGet("/", ([FromServices] ICrewMemberService service) => Results.Ok(service.GetAll()));
    }
}

internal record RegisterCrewMemberRequest(
    Guid CrewMemberId,
    string Name,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] CrewRole Role,
    [property: Required] IReadOnlyCollection<string> Certifications);

internal record SetCrewMemberCertificationsRequest([property: Required] IReadOnlyCollection<string> Certifications);
internal record SetCrewMemberStatusRequest([property: JsonConverter(typeof(JsonStringEnumConverter))] CrewMemberStatus Status);
