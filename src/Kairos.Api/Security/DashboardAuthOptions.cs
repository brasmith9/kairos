namespace Kairos.Api.Security;

/// <summary>
/// Binds the <c>Hangfire:Dashboard</c> configuration section.
/// Leave <see cref="Username"/> empty to restrict the dashboard to local requests.
/// </summary>
public class DashboardAuthOptions
{
    public const string SectionName = "Kairos:Dashboard";

    public string Path { get; set; } = "/dashboard";
    public string? Username { get; set; }
    public string? Password { get; set; }
}
