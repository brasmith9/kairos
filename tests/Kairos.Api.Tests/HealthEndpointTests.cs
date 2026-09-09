using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Kairos.Api.Tests;

[Collection(HangfireStorageCollection.Name)]
public class HealthEndpointTests
{
    // The registered check dials PostgreSQL, which a test has no reason to run. Swapping it
    // for a passing one leaves the thing under test: that /health is mapped and unguarded.
    private static HttpClient ClientWithHealthyChecks() =>
        new KairosAppFactory()
            .WithWebHostBuilder(b => b.ConfigureServices(services =>
                services.Configure<HealthCheckServiceOptions>(o =>
                {
                    o.Registrations.Clear();
                    o.Registrations.Add(new HealthCheckRegistration(
                        "test", _ => new AlwaysHealthy(), HealthStatus.Unhealthy, tags: null));
                })))
            .CreateClient();

    private sealed class AlwaysHealthy : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
            => Task.FromResult(HealthCheckResult.Healthy());
    }

    // The client sends no API key, so a 200 also proves /health sits outside the guard
    // that every /api route lives behind.
    [Fact]
    public async Task ReportsHealthyWhenItsChecksPass()
    {
        var response = await ClientWithHealthyChecks().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
