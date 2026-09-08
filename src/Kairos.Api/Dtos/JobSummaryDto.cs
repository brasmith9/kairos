namespace Kairos.Api.Dtos;

public class JobSummaryDto
{
    public required string Id { get; set; }

    /// <summary>"recurring" for cron jobs, "scheduled" for one-shot jobs.</summary>
    public required string Type { get; set; }

    public string? Cron { get; set; }
    public string? CallbackUrl { get; set; }
    public DateTime? NextExecution { get; set; }
    public DateTime? LastExecution { get; set; }
}
