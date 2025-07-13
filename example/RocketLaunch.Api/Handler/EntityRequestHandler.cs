using Microsoft.AspNetCore.Mvc;
using DDD.BuildingBlocks.Core.ErrorHandling;
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
            try
            {
                var rocket = await service.GetByIdAsync(rocketId);
                return rocket is null ? Results.NotFound() : Results.Ok(rocket);
            }
            catch (ClassifiedErrorException ce)
            {
                return ce.ToApiResult();
            }
        });

        group.MapGet("/rockets", async ([FromServices] IRocketService service) => Results.Ok(await service.GetAllAsync()));

        group.MapGet("/launchpads/{padId:guid}", async ([FromServices] ILaunchPadService service, [FromRoute] Guid padId) =>
        {
            try
            {
                var pad = await service.GetByIdAsync(padId);
                return pad is null ? Results.NotFound() : Results.Ok(pad);
            }
            catch (ClassifiedErrorException ce)
            {
                return ce.ToApiResult();
            }
        });
        group.MapGet("/launchpads", async ([FromServices] ILaunchPadService service) => Results.Ok(await service.GetAllAsync()));
    }
}
