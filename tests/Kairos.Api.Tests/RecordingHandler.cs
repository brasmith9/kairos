using System.Net;

namespace Kairos.Api.Tests;

/// <summary>Captures the outbound callback so tests can assert on what was actually sent.</summary>
internal sealed class RecordingHandler : HttpMessageHandler
{
    public HttpRequestMessage? Request { get; private set; }
    public string? Body { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Request = request;
        Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}
