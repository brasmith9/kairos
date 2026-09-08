using Hangfire.Job.Api.Dtos;
using Hangfire.Job.Api.Services;
using Hangfire.Job.Api.Services.Providers;
using Microsoft.AspNetCore.Mvc;

namespace Hangfire.Job.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController(CallbackUrlValidator callbackUrlValidator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JobRequestDto request)
    {
        var validation = await callbackUrlValidator.ValidateAsync(request.CallbackUrl);
        if (!validation.IsValid)
        {
            return StatusCode(StatusCodes.Status400BadRequest, new { Message = validation.Error });
        }

        RecurringJob.AddOrUpdate<DynamicCallbackJob>(request.UniqueId,
            job => job.ExecuteAndNotifyAsync(request.UniqueId, request.CallbackUrl,
                request.MetaData ?? new Dictionary<string, string>()), request.Cron);

        return StatusCode(StatusCodes.Status201Created, new { Message = "Job created successfully" });
    }

    [HttpDelete("{uniqueId}")]
    public IActionResult DeleteJob(string uniqueId)
    {
        RecurringJob.RemoveIfExists(uniqueId);
        return StatusCode(StatusCodes.Status200OK, new { Message = "Job deleted successfully" });
    }
}
