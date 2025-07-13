using System.Text.Json.Serialization;
using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.Core.Persistence.Storage;
using DDD.BuildingBlocks.DevelopmentPackage.BackgroundService;
using DDD.BuildingBlocks.DevelopmentPackage.EventPublishing;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using DDD.BuildingBlocks.DI.Extensions;
using DDD.BuildingBlocks.Hosting.Background;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using RocketLaunch.Api;
using RocketLaunch.Application;
using RocketLaunch.Api.Handler;
using RocketLaunch.Domain.Service;
using RocketLaunch.ReadModel.Core.Projector.CrewMember;
using RocketLaunch.ReadModel.Core.Projector.Mission;
using RocketLaunch.ReadModel.Core.Service;
using RocketLaunch.ReadModel.InMemory.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
var rocketOptions = builder.Configuration.GetSection("RocketLaunch").Get<RocketLaunchApiOptions>() ?? new RocketLaunchApiOptions();
var workerId = rocketOptions.WorkerId;

builder.Logging.AddConsole();
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var services = builder.Services;
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Rocket Launch API", Version = "v1" });
});
services.Configure<RocketLaunchApiOptions>(builder.Configuration.GetSection("RocketLaunch"));

// Event publishing infrastructure
services.AddSingleton<EventPublishingTable>(x =>
{
    var wt = new EventPublishingTable();
    wt.RegisterWorkerId(workerId);
    
    return wt;
});

services.AddSingleton<DomainEventNotifier>(sp =>
{
    var notifier = new DomainEventNotifier(rocketOptions.ReadModelAssemblyName);
    notifier.SetDependencyResolver(new ServiceLocator(sp));
    return notifier;
});

services.AddSingleton<IDomainEventHandler>(sp =>
    new InProcessDomainEventHandler(sp.GetRequiredService<DomainEventNotifier>(), sp.GetService<ILoggerFactory>()));

services.AddSingleton<DomainEventProjectionDispatcher>(sp =>
    new DomainEventProjectionDispatcher(
        sp.GetRequiredService<IDomainEventHandler>(),
        sp.GetRequiredService<EventPublishingTable>(),
        workerId,
        sp.GetService<ILoggerFactory>()));

services.Configure<TimedHostedServiceOptions>(o =>
    o.GlobalTriggerTimeoutInMilliseconds = rocketOptions.GlobalTriggerTimeoutInMilliseconds);

services.AddHostedService(sp =>
    new TimedHostedService<DomainEventProjectionDispatcher>(
        sp.GetRequiredService<DomainEventProjectionDispatcher>(),
        sp.GetRequiredService<ILoggerFactory>(),
        sp.GetService<IOptions<TimedHostedServiceOptions>>()));

// Event sourcing storage
services.AddSingleton<IEventStorageProvider>(sp =>
    new PureInMemoryEventStorageProvider(sp.GetRequiredService<EventPublishingTable>()));

var snapshotPath = Path.Combine(Path.GetTempPath(), rocketOptions.SnapshotPath);

if (!Path.IsPathRooted(snapshotPath))
{
    snapshotPath = Path.Combine(AppContext.BaseDirectory, snapshotPath);
}

if (!Directory.Exists(snapshotPath))
{
    Directory.CreateDirectory(snapshotPath);
}

services.AddSingleton<ISnapshotStorageProvider>(sp =>
    new InMemorySnapshotStorageProvider(rocketOptions.SnapshotThreshold, snapshotPath));

services.AddSingleton<IEventSourcingRepository>(sp =>
    new EventSourcingRepository(
        sp.GetRequiredService<IEventStorageProvider>(),
        sp.GetRequiredService<ISnapshotStorageProvider>()));

services.AddSingleton<ICommandProcessor, DefaultCommandProcessor>();

// Read model services and validators
services.AddSingleton<IMissionService, InMemoryMissionService>();
services.AddSingleton<IRocketService, InMemoryRocketService>();
services.AddSingleton<ILaunchPadService, InMemoryLaunchPadService>();
services.AddSingleton<ICrewMemberService, InMemoryCrewService>();
services.AddSingleton<IResourceAvailabilityService, InMemoryStationAvailabilityService>();

// Projectors so they can be resolved by the notifier
services.AddTransient<RocketProjector>();
services.AddTransient<LaunchPadProjector>();
services.AddTransient<MissionProjector>();
services.AddTransient<CrewMemberProjector>();

// Domain entry
services.AddSingleton<IDomainEntry>(sp => new DomainEntry(
    sp.GetRequiredService<ICommandProcessor>(),
    sp.GetRequiredService<IEventSourcingRepository>(),
    sp.GetRequiredService<IResourceAvailabilityService>()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// app.Use(async (context, next) =>
// {
//     try
//     {
//         await next();
//         
//         // Log non-successful responses (e.g., 400, 404, etc.)
//         if (context.Response.StatusCode >= 400)
//         {
//             var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
//                 .CreateLogger("GlobalLogger");
//             logger.LogWarning("Request returned status code {StatusCode} for path {Path}", 
//                 context.Response.StatusCode, context.Request.Path);
//         }
//     }
//     catch (Exception ex)
//     {
//         var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
//             .CreateLogger("GlobalException");
//         logger.LogError(ex, "Unhandled exception occurred for path {Path}", context.Request.Path);
//         throw;
//     }
// });

app.UseSwagger();
app.UseSwaggerUI();

// Minimal API endpoints
app.MapMissionRoutes();
app.MapCrewMemberRoutes();
app.MapEntityRoutes();

app.Run();


namespace RocketLaunch.Api
{
    // Placeholder to make Program class public for integration tests if needed
    public partial class Program { }
}
