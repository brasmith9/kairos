namespace Kairos.Api.Security;

/// <summary>
/// Binds the <c>Kairos:Api</c> configuration section.
/// Leave <see cref="Keys"/> empty to restrict the job API to local requests.
/// </summary>
public class ApiAuthOptions
{
    public const string SectionName = "Kairos:Api";

    public string[] Keys { get; set; } = [];
}
