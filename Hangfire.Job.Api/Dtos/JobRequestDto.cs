using System.ComponentModel.DataAnnotations;

namespace Hangfire.Job.Api.Dtos;

public class JobRequestDto
{
    public string? JobName { get; set; } = Guid.NewGuid().ToString("N");
    [Required]public required string UniqueId { get; set; }= null!;
    [Required]public string Cron { get; set; }= null!;
    [Required]public string CallbackUrl { get; set; }= null!;
    public Dictionary<string, string>? MetaData { get; set; }
}