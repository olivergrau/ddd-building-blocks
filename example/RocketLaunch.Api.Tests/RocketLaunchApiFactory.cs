using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using RocketLaunch.Api;

namespace RocketLaunch.Api.Tests;

public class RocketLaunchApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<RocketLaunchApiOptions>(opts =>
            {
                opts.SnapshotPath = Path.GetTempFileName();
                opts.GlobalTriggerTimeoutInMilliseconds = 10;
            });
        });
    }
}
