using System.ComponentModel.DataAnnotations;

namespace Kairos.Api.Dtos;

public class JobRequestDto
{
    /// <summary>
    /// Identifies the job. Scheduling the same id with a cron updates it in place.
    /// </summary>
    [Required] public required string UniqueId { get; set; } = null!;

    [Required] public string CallbackUrl { get; set; } = null!;

    /// <summary>Cron expression for a recurring job. Mutually exclusive with <see cref="RunAt"/>.</summary>
    public string? Cron { get; set; }

    /// <summary>When to fire a one-shot job. Mutually exclusive with <see cref="Cron"/>.</summary>
    public DateTimeOffset? RunAt { get; set; }

    public Dictionary<string, string>? MetaData { get; set; }
}
