using System.Net;
using Kairos.Api.Services;
using Kairos.Api.Services.Providers;

namespace Kairos.Api.Tests;

public class CallbackDeliveryTests
{
    private static DynamicCallbackJob Job(HttpMessageHandler handler, string? signingSecret = null)
    {
        var options = new CallbackOptions { SigningSecret = signingSecret };

        return new DynamicCallbackJob(
            new HttpClient(handler),
            new CallbackUrlValidator(options, _ => Task.FromResult(new[] { IPAddress.Parse("93.184.216.34") })),
            options);
    }

    [Fact]
    public async Task SignsExactlyTheBodyItSends()
    {
        var handler = new RecordingHandler();

        await Job(handler, "topsecret").ExecuteAndNotifyAsync("order-42", "https://example.com/cb", []);

        Assert.Equal(
            CallbackSigner.Sign(handler.Body!, "topsecret"),
            handler.Request!.Headers.GetValues(CallbackSigner.HeaderName).Single());
    }

    [Fact]
    public async Task OmitsTheSignatureHeaderWhenNoSecretIsConfigured()
    {
        var handler = new RecordingHandler();

        await Job(handler).ExecuteAndNotifyAsync("order-42", "https://example.com/cb", []);

        Assert.False(handler.Request!.Headers.Contains(CallbackSigner.HeaderName));
    }

    [Fact]
    public async Task SendsTheUniqueIdAndMetadataAsJson()
    {
        var handler = new RecordingHandler();

        await Job(handler).ExecuteAndNotifyAsync("order-42", "https://example.com/cb",
            new Dictionary<string, string> { ["orderId"] = "42" });

        Assert.Contains("order-42", handler.Body);
        Assert.Contains("orderId", handler.Body);
    }
}
