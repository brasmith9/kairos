using System.Net.Http.Json;
using Hangfire.Job.Api.Dtos;
using Hangfire.Job.Api.Services;

namespace Hangfire.Job.Api.Services.Providers;

public class DynamicCallbackJob(HttpClient httpClient, CallbackUrlValidator callbackUrlValidator)
{
    // Hangfire will execute this method when the cron expression triggers.
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

        var payload = new CallbackPayload
        {
            UniqueId = uniqueId,
            Metadata = metadata
        };

        // Throwing on a failed webhook lets Hangfire apply its retry policy.
        var response = await httpClient.PostAsJsonAsync(callbackUrl, payload);
        response.EnsureSuccessStatusCode();

        RecurringJob.RemoveIfExists(uniqueId);
    }
}
