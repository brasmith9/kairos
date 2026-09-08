using System.Net;
using System.Text;
using Kairos.Api.Security;
using Microsoft.AspNetCore.Http;

namespace Kairos.Api.Tests;

public class DashboardBasicAuthFilterTests
{
    private static HttpContext RequestFrom(IPAddress remoteIp, string? username = null, string? password = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = remoteIp;

        if (username is not null)
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            context.Request.Headers.Authorization = $"Basic {credentials}";
        }

        return context;
    }

    [Fact]
    public void AllowsLoopbackRequest_WhenNoCredentialsConfigured()
    {
        var filter = new DashboardBasicAuthFilter(new DashboardAuthOptions());

        Assert.True(filter.IsAuthorized(RequestFrom(IPAddress.Loopback)));
    }

    [Fact]
    public void DeniesRemoteRequest_WhenNoCredentialsConfigured()
    {
        var filter = new DashboardBasicAuthFilter(new DashboardAuthOptions());

        Assert.False(filter.IsAuthorized(RequestFrom(IPAddress.Parse("203.0.113.5"))));
    }

    [Fact]
    public void AllowsRemoteRequest_WhenCredentialsMatch()
    {
        var filter = new DashboardBasicAuthFilter(new DashboardAuthOptions { Username = "admin", Password = "s3cret" });

        Assert.True(filter.IsAuthorized(RequestFrom(IPAddress.Parse("203.0.113.5"), "admin", "s3cret")));
    }

    [Fact]
    public void DeniesRequest_WhenPasswordIsWrong()
    {
        var filter = new DashboardBasicAuthFilter(new DashboardAuthOptions { Username = "admin", Password = "s3cret" });

        Assert.False(filter.IsAuthorized(RequestFrom(IPAddress.Parse("203.0.113.5"), "admin", "wrong")));
    }

    [Fact]
    public void DeniesLoopbackRequest_WhenCredentialsAreConfiguredButNotSupplied()
    {
        var filter = new DashboardBasicAuthFilter(new DashboardAuthOptions { Username = "admin", Password = "s3cret" });

        Assert.False(filter.IsAuthorized(RequestFrom(IPAddress.Loopback)));
    }

    [Fact]
    public void SendsBasicChallenge_WhenCredentialsAreConfiguredButNotSupplied()
    {
        var filter = new DashboardBasicAuthFilter(new DashboardAuthOptions { Username = "admin", Password = "s3cret" });
        var context = RequestFrom(IPAddress.Loopback);

        filter.IsAuthorized(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("Basic realm=\"Hangfire Dashboard\"", context.Response.Headers.WWWAuthenticate);
    }

    [Fact]
    public void DeniesRequest_WhenAuthorizationHeaderIsMalformed()
    {
        var filter = new DashboardBasicAuthFilter(new DashboardAuthOptions { Username = "admin", Password = "s3cret" });
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.Request.Headers.Authorization = "Basic not-base64!!";

        Assert.False(filter.IsAuthorized(context));
    }
}
