using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RocketLaunch.Application;
using RocketLaunch.Application.Command;
using RocketLaunch.ReadModel.Core.Service;
using RocketLaunch.SharedKernel.Enums;

namespace RocketLaunch.Api.Handler;

internal static class CrewMemberRequestHandler
{
    internal static void MapCrewMemberRoutes(this WebApplication app)
    {
        // app.MapPost("/crew-members", async (HttpRequest request, ILogger<Program> logger) =>
        // {
        //     request.EnableBuffering();
        //
        //     var body = await new StreamReader(request.Body).ReadToEndAsync();
        //     request.Body.Position = 0;
        //
        //     logger.LogInformation("RAW BODY: {Body}", body);
        //
        //     try
        //     {
        //         var obj = JsonSerializer.Deserialize<RegisterCrewMemberRequest>(body, new JsonSerializerOptions
        //         {
        //             PropertyNameCaseInsensitive = true
        //         });
        //
        //         if (obj == null)
        //         {
        //             logger.LogError("Deserialization failed: object is null.");
        //             return Results.BadRequest("Invalid input.");
        //         }
        //
        //         return Results.Ok(obj);
        //     }
        //     catch (Exception ex)
        //     {
        //         logger.LogError(ex, "JSON deserialization error.");
        //         return Results.BadRequest("Deserialization exception: " + ex.Message);
        //     }
        // });

        app.MapPost("/crew-members", async ([FromServices] IDomainEntry entry, [FromBody] RegisterCrewMemberRequest request) =>
        {
            var cmd = new RegisterCrewMemberCommand(request.CrewMemberId, request.Name, request.Role, request.Certifications);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        app.MapPost("/crew-members/{crewMemberId:guid}/assign", async ([FromServices] IDomainEntry entry, [FromRoute] Guid crewMemberId) =>
        {
            var cmd = new AssignCrewMemberCommand(crewMemberId);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        app.MapPost("/crew-members/{crewMemberId:guid}/release", async ([FromServices] IDomainEntry entry, [FromRoute] Guid crewMemberId) =>
        {
            var cmd = new ReleaseCrewMemberCommand(crewMemberId);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        app.MapPost("/crew-members/{crewMemberId:guid}/certifications", async ([FromServices] IDomainEntry entry, [FromRoute] Guid crewMemberId, [FromBody] SetCrewMemberCertificationsRequest request) =>
        {
            var cmd = new SetCrewMemberCertificationsCommand(crewMemberId, request.Certifications);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        app.MapPost("/crew-members/{crewMemberId:guid}/status", async ([FromServices] IDomainEntry entry, [FromRoute] Guid crewMemberId, [FromBody] SetCrewMemberStatusRequest request) =>
        {
            var cmd = new SetCrewMemberStatusCommand(crewMemberId, request.Status);
            var result = await entry.ExecuteAsync(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.FailReason);
        });

        app.MapGet("/crew-members/{crewMemberId:guid}", ([FromServices] ICrewMemberService service, [FromRoute] Guid crewMemberId) =>
        {
            var crew = service.GetById(crewMemberId);
            return crew is null ? Results.NotFound() : Results.Ok(crew);
        });
    }
}

internal record RegisterCrewMemberRequest(
    Guid CrewMemberId,
    string Name,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] CrewRole Role,
    [property: Required] IReadOnlyCollection<string> Certifications);

internal record SetCrewMemberCertificationsRequest([property: Required] IReadOnlyCollection<string> Certifications);
internal record SetCrewMemberStatusRequest([property: JsonConverter(typeof(JsonStringEnumConverter))] CrewMemberStatus Status);
