using System.Net;
using Kairos.Api.Security;
using Microsoft.AspNetCore.Http;

namespace Kairos.Api.Tests;

public class ApiKeyAuthenticatorTests
{
    private static HttpContext RequestFrom(IPAddress remoteIp, string? apiKey = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = remoteIp;
        if (apiKey is not null) context.Request.Headers[ApiKeyAuthenticator.HeaderName] = apiKey;
        return context;
    }

    private static readonly IPAddress Remote = IPAddress.Parse("203.0.113.5");

    [Fact]
    public void AllowsLoopbackRequest_WhenNoKeysConfigured()
    {
        var authenticator = new ApiKeyAuthenticator(new ApiAuthOptions());

        Assert.True(authenticator.IsAuthorized(RequestFrom(IPAddress.Loopback)));
    }

    [Fact]
    public void DeniesRemoteRequest_WhenNoKeysConfigured()
    {
        var authenticator = new ApiKeyAuthenticator(new ApiAuthOptions());

        Assert.False(authenticator.IsAuthorized(RequestFrom(Remote)));
    }

    [Fact]
    public void AllowsRemoteRequest_WhenKeyMatches()
    {
        var authenticator = new ApiKeyAuthenticator(new ApiAuthOptions { Keys = ["k-one"] });

        Assert.True(authenticator.IsAuthorized(RequestFrom(Remote, "k-one")));
    }

    [Fact]
    public void AllowsRequest_WhenAnyConfiguredKeyMatches()
    {
        var authenticator = new ApiKeyAuthenticator(new ApiAuthOptions { Keys = ["k-one", "k-two"] });

        Assert.True(authenticator.IsAuthorized(RequestFrom(Remote, "k-two")));
    }

    [Fact]
    public void DeniesRequest_WhenKeyIsWrong()
    {
        var authenticator = new ApiKeyAuthenticator(new ApiAuthOptions { Keys = ["k-one"] });

        Assert.False(authenticator.IsAuthorized(RequestFrom(Remote, "nope")));
    }

    [Fact]
    public void DeniesRequest_WhenHeaderIsMissing()
    {
        var authenticator = new ApiKeyAuthenticator(new ApiAuthOptions { Keys = ["k-one"] });

        Assert.False(authenticator.IsAuthorized(RequestFrom(Remote)));
    }

    [Fact]
    public void DeniesLoopbackRequest_WhenKeysAreConfiguredButNoneSupplied()
    {
        var authenticator = new ApiKeyAuthenticator(new ApiAuthOptions { Keys = ["k-one"] });

        Assert.False(authenticator.IsAuthorized(RequestFrom(IPAddress.Loopback)));
    }
}
