using System.Net;
using System.Text.Json;

namespace Kairos.Api.Tests;

[Collection(HangfireStorageCollection.Name)]
public class OpenApiEndpointTests
{
    private static async Task<JsonElement> DocumentAsync()
    {
        var response = await new KairosAppFactory().CreateClient().GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    [Fact]
    public async Task IsServedOutsideDevelopment()
    {
        var response = await new KairosAppFactory().CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DescribesEveryJobsRoute()
    {
        var paths = (await DocumentAsync()).GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/jobs", out _));
        Assert.True(paths.TryGetProperty("/api/jobs/{id}", out _));
    }

    [Fact]
    public async Task MarksTheFieldsAJobRequestCannotOmit()
    {
        // SuppressImplicitRequiredAttributeForNonNullableReferenceTypes means nothing is
        // required unless it says so, and a generated client would happily post an empty body.
        var required = (await DocumentAsync())
            .GetProperty("components").GetProperty("schemas")
            .GetProperty("JobRequestDto").GetProperty("required")
            .EnumerateArray().Select(e => e.GetString()).ToList();

        Assert.Contains("uniqueId", required);
        Assert.Contains("callbackUrl", required);
    }

    [Fact]
    public async Task NamesTheApiSoGeneratedClientsAreNotCalledAfterTheAssembly()
    {
        var info = (await DocumentAsync()).GetProperty("info");

        Assert.Equal("Kairos", info.GetProperty("title").GetString());
    }
}
