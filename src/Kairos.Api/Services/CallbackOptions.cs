namespace Kairos.Api.Services;

/// <summary>
/// Binds the <c>Hangfire:Callbacks</c> configuration section.
/// </summary>
public class CallbackOptions
{
    public const string SectionName = "Kairos:Callbacks";

    /// <summary>
    /// Allows callbacks to loopback and private-network addresses.
    /// Enable this for local development only - it disables the SSRF guard.
    /// </summary>
    public bool AllowPrivateNetworks { get; set; }

    /// <summary>
    /// Shared secret used to sign callback bodies. When empty, callbacks are unsigned.
    /// </summary>
    public string? SigningSecret { get; set; }
}
