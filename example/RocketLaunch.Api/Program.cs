using System.Text.Json.Serialization;
using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Persistence.Repository;
using DDD.BuildingBlocks.Core.Persistence.Storage;
using DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;
using DDD.BuildingBlocks.DevelopmentPackage.BackgroundService;
using DDD.BuildingBlocks.DevelopmentPackage.EventPublishing;
using DDD.BuildingBlocks.DevelopmentPackage.Storage;
using DDD.BuildingBlocks.DI.Extensions;
using DDD.BuildingBlocks.Hosting.Background;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RocketLaunch.Application;
using RocketLaunch.Api.Handler;
using RocketLaunch.Domain.Service;
using RocketLaunch.ReadModel.Core.Builder;
using RocketLaunch.ReadModel.Core.Service;
using RocketLaunch.ReadModel.InMemory.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

var services = builder.Services;

// Event publishing infrastructure
services.AddSingleton<EventPublishingTable>();

services.AddSingleton<DomainEventNotifier>(sp =>
{
    var notifier = new DomainEventNotifier("RocketLaunch");
    notifier.SetDependencyResolver(new ServiceLocator(sp));
    return notifier;
});

services.AddSingleton<IDomainEventHandler>(sp =>
    new InProcessDomainEventHandler(sp.GetRequiredService<DomainEventNotifier>(), sp.GetService<ILoggerFactory>()));

services.AddSingleton<DomainEventProjectionDispatcher>(sp =>
    new DomainEventProjectionDispatcher(
        sp.GetRequiredService<IDomainEventHandler>(),
        sp.GetRequiredService<EventPublishingTable>(),
        "projection-worker",
        sp.GetService<ILoggerFactory>()));

services.Configure<TimedHostedServiceOptions>(o => o.GlobalTriggerTimeoutInMilliseconds = 100);

services.AddHostedService(sp =>
    new TimedHostedService<DomainEventProjectionDispatcher>(
        sp.GetRequiredService<DomainEventProjectionDispatcher>(),
        sp.GetRequiredService<ILoggerFactory>(),
        sp.GetService<IOptions<TimedHostedServiceOptions>>()));

// Event sourcing storage
services.AddSingleton<IEventStorageProvider>(sp =>
    new PureInMemoryEventStorageProvider(sp.GetRequiredService<EventPublishingTable>()));

var snapshotPath = Path.Combine(AppContext.BaseDirectory, "mission.snapshots.dump");
services.AddSingleton<ISnapshotStorageProvider>(sp => new InMemorySnapshotStorageProvider(5, snapshotPath));

services.AddSingleton<IEventSourcingRepository>(sp =>
    new EventSourcingRepository(
        sp.GetRequiredService<IEventStorageProvider>(),
        sp.GetRequiredService<ISnapshotStorageProvider>()));

services.AddSingleton<ICommandProcessor, DefaultCommandProcessor>();

// Read model services and validators
services.AddSingleton<IRocketService, InMemoryRocketService>();
services.AddSingleton<ILaunchPadService, InMemoryLaunchPadService>();
services.AddSingleton<ICrewMemberService, InMemoryCrewService>();
services.AddSingleton<IResourceAvailabilityService, InMemoryStationAvailabilityService>();

// Projectors so they can be resolved by the notifier
services.AddTransient<RocketProjector>();
services.AddTransient<LaunchPadProjector>();
services.AddTransient<CrewMemberProjector>();

// Domain entry
services.AddSingleton<IDomainEntry>(sp => new DomainEntry(
    sp.GetRequiredService<ICommandProcessor>(),
    sp.GetRequiredService<IEventSourcingRepository>(),
    sp.GetRequiredService<IResourceAvailabilityService>()));

var app = builder.Build();

// Minimal API endpoints
app.MapMissionRoutes();

app.Run();


namespace RocketLaunch.Api
{
    // Placeholder to make Program class public for integration tests if needed
    public partial class Program { }
}
