using System.Net;
using Hangfire.Job.Api.Controllers;
using Hangfire.Job.Api.Dtos;
using Hangfire.Job.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hangfire.Job.Api.Tests;

public class JobsControllerTests
{
    private static JobsController ControllerResolving(string address) =>
        new(new CallbackUrlValidator(
            new CallbackOptions(),
            _ => Task.FromResult(new[] { IPAddress.Parse(address) })));

    private static JobRequestDto Request(string callbackUrl) => new()
    {
        UniqueId = "order-1",
        Cron = "* * * * *",
        CallbackUrl = callbackUrl
    };

    [Fact]
    public async Task RejectsMalformedCallbackUrl()
    {
        var result = await ControllerResolving("93.184.216.34").Create(Request("not a url"));

        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task RejectsCallbackUrlPointingAtPrivateNetwork()
    {
        var result = await ControllerResolving("10.0.0.5").Create(Request("http://10.0.0.5/callback"));

        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<ObjectResult>(result).StatusCode);
    }
}
