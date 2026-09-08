using System.Net;
using Kairos.Api.Services;
using Kairos.Api.Services.Providers;

namespace Kairos.Api.Tests;

public class DynamicCallbackJobTests
{
    [Fact]
    public async Task RefusesToPostToUrlThatIsNoLongerAllowed()
    {
        // A hostname that passed validation when the job was created can later
        // resolve to a private address (DNS rebinding), so the job re-validates.
        var options = new CallbackOptions();
        var job = new DynamicCallbackJob(
            new HttpClient(),
            new CallbackUrlValidator(options, _ => Task.FromResult(new[] { IPAddress.Parse("169.254.169.254") })),
            options);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            job.ExecuteAndNotifyAsync("order-1", "http://metadata.example.com/", []));
    }
}
