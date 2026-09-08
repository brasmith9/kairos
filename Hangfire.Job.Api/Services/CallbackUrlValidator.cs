using System.Net;
using System.Net.Sockets;

namespace Hangfire.Job.Api.Services;

public record CallbackUrlValidationResult(bool IsValid, string? Error)
{
    public static CallbackUrlValidationResult Valid() => new(true, null);
    public static CallbackUrlValidationResult Invalid(string error) => new(false, error);
}

/// <summary>
/// Checks that a caller-supplied callback URL is safe to request.
/// Callers control the URL, so without this an unauthenticated request could make the
/// server probe internal hosts or read cloud metadata (SSRF).
/// </summary>
public class CallbackUrlValidator
{
    private readonly CallbackOptions _options;
    private readonly Func<string, Task<IPAddress[]>> _resolveHost;

    public CallbackUrlValidator(CallbackOptions options, Func<string, Task<IPAddress[]>>? resolveHost = null)
    {
        _options = options;
        _resolveHost = resolveHost ?? Dns.GetHostAddressesAsync;
    }

    public async Task<CallbackUrlValidationResult> ValidateAsync(string? callbackUrl)
    {
        if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out var uri))
        {
            return CallbackUrlValidationResult.Invalid("Callback URL must be an absolute URI.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return CallbackUrlValidationResult.Invalid("Callback URL must use http or https.");
        }

        if (_options.AllowPrivateNetworks)
        {
            return CallbackUrlValidationResult.Valid();
        }

        IPAddress[] addresses;
        try
        {
            addresses = await _resolveHost(uri.Host);
        }
        catch (SocketException)
        {
            return CallbackUrlValidationResult.Invalid("Callback URL host could not be resolved.");
        }

        if (addresses.Length == 0)
        {
            return CallbackUrlValidationResult.Invalid("Callback URL host could not be resolved.");
        }

        return addresses.Any(IsPrivate)
            ? CallbackUrlValidationResult.Invalid(
                "Callback URL must not resolve to a loopback, private or link-local address.")
            : CallbackUrlValidationResult.Valid();
    }

    private static bool IsPrivate(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address)) return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var octets = address.GetAddressBytes();
            return octets[0] switch
            {
                0 or 10 => true,
                100 => octets[1] >= 64 && octets[1] <= 127, // carrier-grade NAT
                169 => octets[1] == 254,                    // link-local, incl. cloud metadata
                172 => octets[1] >= 16 && octets[1] <= 31,
                192 => octets[1] == 168,
                >= 224 => true,                             // multicast and reserved
                _ => false
            };
        }

        return address.IsIPv6LinkLocal
               || address.IsIPv6SiteLocal
               || address.IsIPv6Multicast
               || (address.GetAddressBytes()[0] & 0xFE) == 0xFC // unique local fc00::/7
               || address.Equals(IPAddress.IPv6Any);
    }
}
