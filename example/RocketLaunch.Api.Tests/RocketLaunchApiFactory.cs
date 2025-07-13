using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
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
            
            services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.PropertyNameCaseInsensitive = true;
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        });
    }
}
