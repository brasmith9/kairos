using System.Net;

namespace Kairos.Api.Security;

/// <summary>
/// Fallback used by the dashboard and API guards when no credentials are configured:
/// serve local callers, refuse everyone else.
/// </summary>
public static class LocalRequest
{
    public static bool IsLocal(HttpContext httpContext)
    {
        var remote = httpContext.Connection.RemoteIpAddress;
        var local = httpContext.Connection.LocalIpAddress;

        if (remote is null) return local is null;
        if (local is not null) return remote.Equals(local);

        return IPAddress.IsLoopback(remote);
    }
}
