using System.Security.Cryptography;
using System.Text;

namespace Kairos.Api.Services;

/// <summary>
/// Signs callback bodies so a receiver can verify the request came from this instance.
/// </summary>
public static class CallbackSigner
{
    public const string HeaderName = "X-Kairos-Signature";

    public static string Sign(string payload, string secret)
    {
        var digest = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));

        return $"sha256={Convert.ToHexString(digest).ToLowerInvariant()}";
    }
}
