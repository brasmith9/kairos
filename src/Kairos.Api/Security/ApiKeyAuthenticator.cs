using System.Security.Cryptography;
using System.Text;

namespace Kairos.Api.Security;

/// <summary>
/// Guards the job endpoints with a shared API key. Scheduling a job makes the server
/// issue an outbound request, so these endpoints are never open by default: with no keys
/// configured only local callers are served.
/// </summary>
public class ApiKeyAuthenticator(ApiAuthOptions options)
{
    public const string HeaderName = "X-Api-Key";

    public bool IsAuthorized(HttpContext httpContext)
    {
        if (options.Keys.Length == 0)
        {
            return LocalRequest.IsLocal(httpContext);
        }

        var supplied = httpContext.Request.Headers[HeaderName].ToString();

        return supplied.Length > 0 && options.Keys.Any(key => Matches(supplied, key));
    }

    private static bool Matches(string supplied, string configured) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(supplied),
            Encoding.UTF8.GetBytes(configured));
}
