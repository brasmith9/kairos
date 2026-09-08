using System.Net;
using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;

namespace Hangfire.Job.Api.Security;

/// <summary>
/// Guards the Hangfire dashboard with HTTP Basic authentication.
/// When no username is configured the dashboard is served to local requests only,
/// so an unconfigured deployment is never exposed to the internet.
/// </summary>
public class DashboardBasicAuthFilter(DashboardAuthOptions options) : IDashboardAuthorizationFilter
{
    private const string Challenge = "Basic realm=\"Hangfire Dashboard\"";

    public bool Authorize(DashboardContext context) => IsAuthorized(context.GetHttpContext());

    public bool IsAuthorized(HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(options.Username))
        {
            return IsLocalRequest(httpContext);
        }

        if (TryReadBasicCredentials(httpContext.Request.Headers.Authorization.ToString(), out var user, out var password)
            && MatchesConfigured(user, options.Username)
            && MatchesConfigured(password, options.Password ?? string.Empty))
        {
            return true;
        }

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        httpContext.Response.Headers.WWWAuthenticate = Challenge;
        return false;
    }

    private static bool IsLocalRequest(HttpContext httpContext)
    {
        var remote = httpContext.Connection.RemoteIpAddress;
        var local = httpContext.Connection.LocalIpAddress;

        if (remote is null) return local is null;
        if (local is not null) return remote.Equals(local);

        return IPAddress.IsLoopback(remote);
    }

    private static bool TryReadBasicCredentials(string header, out string username, out string password)
    {
        username = password = string.Empty;

        if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return false;

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..].Trim()));
        }
        catch (FormatException)
        {
            return false;
        }

        var separator = decoded.IndexOf(':');
        if (separator < 0) return false;

        username = decoded[..separator];
        password = decoded[(separator + 1)..];
        return true;
    }

    private static bool MatchesConfigured(string supplied, string configured) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(supplied),
            Encoding.UTF8.GetBytes(configured));
}
