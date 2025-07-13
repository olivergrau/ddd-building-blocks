using Microsoft.AspNetCore.Mvc;
using RocketLaunch.ReadModel.Core.Service;

namespace RocketLaunch.Api.Handler;

internal static class EntityRequestHandler
{
    internal static void MapEntityRoutes(this WebApplication app)
    {
        var group = app.MapGroup("entities")
            .WithTags("Entities");

        group.MapGet("/rockets/{rocketId:guid}", async ([FromServices] IRocketService service, [FromRoute] Guid rocketId) =>
        {
            var rocket = await service.GetByIdAsync(rocketId);
            return rocket is null ? Results.NotFound() : Results.Ok(rocket);
        });

        group.MapGet("/rockets", async ([FromServices] IRocketService service) => Results.Ok(await service.GetAllAsync()));

        group.MapGet("/launchpads/{padId:guid}", async ([FromServices] ILaunchPadService service, [FromRoute] Guid padId) =>
        {
            var pad = await service.GetByIdAsync(padId);
            return pad is null ? Results.NotFound() : Results.Ok(pad);
        });
        group.MapGet("/launchpads", async ([FromServices] ILaunchPadService service) => Results.Ok(await service.GetAllAsync()));
    }
}
