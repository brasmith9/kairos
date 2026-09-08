using Hangfire;
using System.Net;
using Hangfire.InMemory;
using Kairos.Api.Controllers;
using Kairos.Api.Dtos;
using Kairos.Api.Services;
using Hangfire.Storage;

namespace Kairos.Api.Tests;

/// <summary>
/// Exercises scheduling and cancellation against real (in-memory) Hangfire storage,
/// so the recurring-job id the API writes and the one it deletes have to agree.
/// </summary>
[Collection(HangfireStorageCollection.Name)]
public class RecurringJobLifecycleTests
{
    private static JobsController Controller()
    {
        JobStorage.Current = new InMemoryStorage();

        return new JobsController(new CallbackUrlValidator(
            new CallbackOptions(),
            _ => Task.FromResult(new[] { IPAddress.Parse("93.184.216.34") })));
    }

    private static List<RecurringJobDto> ScheduledJobs()
    {
        using var connection = JobStorage.Current.GetConnection();
        return connection.GetRecurringJobs();
    }

    private static JobRequestDto Request(string uniqueId) => new()
    {
        UniqueId = uniqueId,
        Cron = "*/5 * * * *",
        CallbackUrl = "https://example.com/webhooks/hangfire"
    };

    [Fact]
    public async Task SchedulesJobUnderTheCallerSuppliedUniqueId()
    {
        var controller = Controller();

        await controller.Create(Request("order-42"));

        Assert.Equal("order-42", Assert.Single(ScheduledJobs()).Id);
    }

    [Fact]
    public async Task DeletesTheJobItScheduled()
    {
        var controller = Controller();
        await controller.Create(Request("order-42"));

        controller.DeleteJob("order-42");

        Assert.Empty(ScheduledJobs());
    }

    [Fact]
    public async Task ReschedulingTheSameUniqueIdUpdatesInsteadOfDuplicating()
    {
        var controller = Controller();

        await controller.Create(Request("order-42"));
        await controller.Create(Request("order-42"));

        Assert.Single(ScheduledJobs());
    }
}
