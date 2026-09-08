using Hangfire;
using Hangfire.Storage;
using Kairos.Api.Dtos;
using Kairos.Api.Services;
using Kairos.Api.Services.Providers;
using Microsoft.AspNetCore.Mvc;

namespace Kairos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController(CallbackUrlValidator callbackUrlValidator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JobRequestDto request)
    {
        var isRecurring = !string.IsNullOrWhiteSpace(request.Cron);
        if (isRecurring == request.RunAt.HasValue)
        {
            return StatusCode(StatusCodes.Status400BadRequest,
                new { Message = "Provide exactly one of cron or runAt." });
        }

        var validation = await callbackUrlValidator.ValidateAsync(request.CallbackUrl);
        if (!validation.IsValid)
        {
            return StatusCode(StatusCodes.Status400BadRequest, new { Message = validation.Error });
        }

        var metadata = request.MetaData ?? new Dictionary<string, string>();

        if (isRecurring)
        {
            RecurringJob.AddOrUpdate<DynamicCallbackJob>(request.UniqueId,
                job => job.ExecuteAndNotifyAsync(request.UniqueId, request.CallbackUrl, metadata),
                request.Cron!);

            return StatusCode(StatusCodes.Status201Created,
                new { Id = request.UniqueId, Message = "Job scheduled successfully" });
        }

        var jobId = BackgroundJob.Schedule<DynamicCallbackJob>(
            job => job.ExecuteAndNotifyAsync(request.UniqueId, request.CallbackUrl, metadata),
            request.RunAt!.Value);

        return StatusCode(StatusCodes.Status201Created,
            new { Id = jobId, Message = "Job scheduled successfully" });
    }

    [HttpGet]
    public IActionResult List()
    {
        using var connection = JobStorage.Current.GetConnection();

        var recurring = connection.GetRecurringJobs().Select(job => new JobSummaryDto
        {
            Id = job.Id,
            Type = "recurring",
            Cron = job.Cron,
            CallbackUrl = CallbackUrlOf(job.Job),
            NextExecution = job.NextExecution,
            LastExecution = job.LastExecution
        });

        var scheduled = JobStorage.Current.GetMonitoringApi().ScheduledJobs(0, 500).Select(entry =>
            new JobSummaryDto
            {
                Id = entry.Key,
                Type = "scheduled",
                CallbackUrl = CallbackUrlOf(entry.Value.Job),
                NextExecution = entry.Value.EnqueueAt
            });

        return Ok(recurring.Concat(scheduled).ToList());
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteJob(string id)
    {
        using (var connection = JobStorage.Current.GetConnection())
        {
            if (connection.GetRecurringJobs().Any(job => job.Id == id))
            {
                RecurringJob.RemoveIfExists(id);

                return StatusCode(StatusCodes.Status200OK, new { Message = "Job deleted successfully" });
            }
        }

        try
        {
            BackgroundJob.Delete(id);
        }
        catch (BackgroundJobClientException exception) when (exception.InnerException is FormatException)
        {
            // Storages key one-shot jobs by their own id format - PostgreSQL parses them as
            // bigint - so an id that is neither a recurring job nor parseable is simply unknown.
            return StatusCode(StatusCodes.Status404NotFound, new { Message = $"No job with id '{id}'." });
        }

        return StatusCode(StatusCodes.Status200OK, new { Message = "Job deleted successfully" });
    }

    // The callback URL is the second argument of ExecuteAndNotifyAsync.
    private static string? CallbackUrlOf(Hangfire.Common.Job? job) => job?.Args.ElementAtOrDefault(1) as string;
}
