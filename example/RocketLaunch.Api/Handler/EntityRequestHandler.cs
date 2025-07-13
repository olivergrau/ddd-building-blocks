using Microsoft.AspNetCore.Mvc;
using RocketLaunch.ReadModel.Core.Service;

namespace RocketLaunch.Api.Handler;

internal static class EntityRequestHandler
{
    internal static void MapEntityRoutes(this WebApplication app)
    {
        var group = app.MapGroup("entities")
            .WithTags("Entities");

        group.MapGet("/rockets/{rocketId:guid}", ([FromServices] IRocketService service, [FromRoute] Guid rocketId) =>
        {
            var rocket = service.GetById(rocketId);
            return rocket is null ? Results.NotFound() : Results.Ok(rocket);
        });

        group.MapGet("/rockets", ([FromServices] IRocketService service) => Results.Ok(service.GetAll()));

        group.MapGet("/launchpads/{padId:guid}", ([FromServices] ILaunchPadService service, [FromRoute] Guid padId) =>
        {
            var pad = service.GetById(padId);
            return pad is null ? Results.NotFound() : Results.Ok(pad);
        });

        group.MapGet("/launchpads", ([FromServices] ILaunchPadService service) => Results.Ok(service.GetAll()));
    }
}
