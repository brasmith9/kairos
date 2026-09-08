using System.ComponentModel.DataAnnotations;

namespace Hangfire.Job.Api.Dtos;

public class JobRequestDto
{
    /// <summary>
    /// Identifies the recurring job. Scheduling the same id again updates it in place.
    /// </summary>
    [Required] public required string UniqueId { get; set; } = null!;

    [Required] public string Cron { get; set; } = null!;
    [Required] public string CallbackUrl { get; set; } = null!;
    public Dictionary<string, string>? MetaData { get; set; }
}
