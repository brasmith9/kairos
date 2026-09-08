using System.Net;
using Hangfire.Job.Api.Services;
using Hangfire.Job.Api.Services.Providers;

namespace Hangfire.Job.Api.Tests;

public class DynamicCallbackJobTests
{
    [Fact]
    public async Task RefusesToPostToUrlThatIsNoLongerAllowed()
    {
        // A hostname that passed validation when the job was created can later
        // resolve to a private address (DNS rebinding), so the job re-validates.
        var validator = new CallbackUrlValidator(
            new CallbackOptions(),
            _ => Task.FromResult(new[] { IPAddress.Parse("169.254.169.254") }));
        var job = new DynamicCallbackJob(new HttpClient(), validator);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            job.ExecuteAndNotifyAsync("job-1", "order-1", "http://metadata.example.com/", []));
    }
}
