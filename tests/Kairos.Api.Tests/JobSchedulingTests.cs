using System.Net;
using Hangfire;
using Hangfire.InMemory;
using Kairos.Api.Controllers;
using Kairos.Api.Dtos;
using Kairos.Api.Services;
using Kairos.Api.Services.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kairos.Api.Tests;

/// <summary>
/// Scheduling, listing and cancellation against real (in-memory) Hangfire storage.
/// </summary>
[Collection(HangfireStorageCollection.Name)]
public class JobSchedulingTests
{
    private static JobsController Controller()
    {
        JobStorage.Current = new InMemoryStorage();

        return new JobsController(new CallbackUrlValidator(
            new CallbackOptions(),
            _ => Task.FromResult(new[] { IPAddress.Parse("93.184.216.34") })));
    }

    private static JobRequestDto Recurring(string uniqueId, string cron = "*/5 * * * *") => new()
    {
        UniqueId = uniqueId, Cron = cron, CallbackUrl = "https://example.com/webhooks/kairos"
    };

    private static JobRequestDto OneShot(string uniqueId, DateTimeOffset runAt) => new()
    {
        UniqueId = uniqueId, RunAt = runAt, CallbackUrl = "https://example.com/webhooks/kairos"
    };

    private static List<JobSummaryDto> Listed(JobsController controller) =>
        Assert.IsType<List<JobSummaryDto>>(Assert.IsType<OkObjectResult>(controller.List()).Value);

    private static int StatusOf(IActionResult result) => Assert.IsType<ObjectResult>(result).StatusCode!.Value;

    [Fact]
    public async Task RejectsRequestWithNeitherCronNorRunAt()
    {
        var request = new JobRequestDto { UniqueId = "order-42", CallbackUrl = "https://example.com/cb" };

        Assert.Equal(StatusCodes.Status400BadRequest, StatusOf(await Controller().Create(request)));
    }

    [Fact]
    public async Task RejectsRequestWithBothCronAndRunAt()
    {
        var request = Recurring("order-42");
        request.RunAt = DateTimeOffset.UtcNow.AddHours(1);

        Assert.Equal(StatusCodes.Status400BadRequest, StatusOf(await Controller().Create(request)));
    }

    [Fact]
    public async Task SchedulesAOneShotJobForRunAt()
    {
        var controller = Controller();

        await controller.Create(OneShot("order-42", DateTimeOffset.UtcNow.AddHours(1)));

        Assert.Equal("scheduled", Assert.Single(Listed(controller)).Type);
    }

    [Fact]
    public async Task ListsRecurringJobsWithTheirCronAndCallbackUrl()
    {
        var controller = Controller();
        await controller.Create(Recurring("order-42", "0 * * * *"));

        var job = Assert.Single(Listed(controller));

        Assert.Equal("order-42", job.Id);
        Assert.Equal("recurring", job.Type);
        Assert.Equal("0 * * * *", job.Cron);
        Assert.Equal("https://example.com/webhooks/kairos", job.CallbackUrl);
    }

    [Fact]
    public async Task LeavesARecurringJobInPlaceAfterASuccessfulCallback()
    {
        var controller = Controller();
        await controller.Create(Recurring("order-42"));
        var options = new CallbackOptions();
        var job = new DynamicCallbackJob(
            new HttpClient(new RecordingHandler()),
            new CallbackUrlValidator(options, _ => Task.FromResult(new[] { IPAddress.Parse("93.184.216.34") })),
            options);

        await job.ExecuteAndNotifyAsync("order-42", "https://example.com/webhooks/kairos", []);

        Assert.Single(Listed(controller));
    }

    [Fact]
    public void ListsNothingWhenNothingIsScheduled()
    {
        Assert.Empty(Listed(Controller()));
    }

    [Fact]
    public async Task CancelsAOneShotJobByTheIdItReturned()
    {
        var controller = Controller();
        var created = await controller.Create(OneShot("order-42", DateTimeOffset.UtcNow.AddHours(1)));
        var id = Assert.Single(Listed(controller)).Id;

        Assert.Equal(StatusCodes.Status201Created, StatusOf(created));
        controller.DeleteJob(id);

        Assert.Empty(Listed(controller));
    }
}
