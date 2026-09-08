using System.Text;
using System.Text.Json;
using Kairos.Api.Dtos;
using Kairos.Api.Services;

namespace Kairos.Api.Services.Providers;

public class DynamicCallbackJob(
    HttpClient httpClient,
    CallbackUrlValidator callbackUrlValidator,
    CallbackOptions options)
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // Hangfire executes this when the cron fires, or once at the requested time.
    public async Task ExecuteAndNotifyAsync(string uniqueId, string callbackUrl,
        Dictionary<string, string> metadata)
    {
        // Re-check the URL at execution time: the host may have been re-pointed at a
        // private address since the job was created.
        var validation = await callbackUrlValidator.ValidateAsync(callbackUrl);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Refusing to call back to '{callbackUrl}'. {validation.Error}");
        }

        var body = JsonSerializer.Serialize(
            new CallbackPayload { UniqueId = uniqueId, Metadata = metadata }, JsonOptions);

        var request = new HttpRequestMessage(HttpMethod.Post, callbackUrl)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(options.SigningSecret))
        {
            request.Headers.Add(CallbackSigner.HeaderName, CallbackSigner.Sign(body, options.SigningSecret));
        }

        // Throwing on a failed webhook lets Hangfire apply its retry policy.
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
