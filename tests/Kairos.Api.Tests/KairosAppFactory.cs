using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kairos.Api.Tests;

/// <summary>
/// Boots the real application for endpoint tests. Hangfire's PostgreSQL storage dials the
/// database while the app is still starting - with no database reachable it blocks before
/// Kestrel binds, and the configured Timeout=30 costs half a minute per boot. Storage is
/// swapped for the in-memory one, and the registrations that would build the PostgreSQL
/// one (or run a background server with nothing to do) are dropped.
/// </summary>
public class KairosAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Production is the point of the OpenAPI test: the doc used to be Development-only.
        builder.UseEnvironment("Production");

        builder.ConfigureServices(services =>
        {
            JobStorage.Current = new InMemoryStorage();

            foreach (var descriptor in services
                         .Where(d => d.ServiceType == typeof(JobStorage) ||
                                     d.ServiceType == typeof(IGlobalConfiguration) ||
                                     (d.ServiceType == typeof(IHostedService) &&
                                      d.ImplementationType?.Namespace?.StartsWith("Hangfire") == true))
                         .ToList())
            {
                services.Remove(descriptor);
            }

            services.AddSingleton(JobStorage.Current);

            // UseHangfireDashboard refuses to start without this, but the original
            // registration is what builds the PostgreSQL storage, so hand back the bare
            // static configuration instead.
            services.AddSingleton<IGlobalConfiguration>(GlobalConfiguration.Configuration);
        });
    }
}
