using System.Net;
using Kairos.Api.Services;

namespace Kairos.Api.Tests;

public class CallbackUrlValidatorTests
{
    private static CallbackUrlValidator Validator(
        bool allowPrivateNetworks = false,
        params string[] resolvesTo)
    {
        var addresses = resolvesTo.Select(IPAddress.Parse).ToArray();
        return new CallbackUrlValidator(
            new CallbackOptions { AllowPrivateNetworks = allowPrivateNetworks },
            _ => Task.FromResult(addresses));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/relative/path")]
    [InlineData("not a url")]
    public async Task RejectsUrlThatIsNotAbsolute(string? url)
    {
        var result = await Validator(resolvesTo: "93.184.216.34").ValidateAsync(url);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("ftp://example.com/callback")]
    [InlineData("file:///etc/passwd")]
    public async Task RejectsSchemeOtherThanHttpOrHttps(string url)
    {
        var result = await Validator(resolvesTo: "93.184.216.34").ValidateAsync(url);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task RejectsLoopbackAddress()
    {
        var result = await Validator(resolvesTo: "127.0.0.1").ValidateAsync("http://127.0.0.1/callback");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task RejectsPrivateNetworkAddress()
    {
        var result = await Validator(resolvesTo: "10.0.0.5").ValidateAsync("http://10.0.0.5/callback");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task RejectsCloudMetadataLinkLocalAddress()
    {
        var result = await Validator(resolvesTo: "169.254.169.254")
            .ValidateAsync("http://169.254.169.254/latest/meta-data/");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task RejectsHostnameThatResolvesToPrivateAddress()
    {
        var result = await Validator(resolvesTo: "192.168.1.10").ValidateAsync("https://internal.example.com/callback");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task RejectsHostThatDoesNotResolve()
    {
        var result = await Validator().ValidateAsync("https://nowhere.example.com/callback");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AllowsHostnameThatResolvesToPublicAddress()
    {
        var result = await Validator(resolvesTo: "93.184.216.34").ValidateAsync("https://example.com/callback");

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task AllowsPrivateAddressWhenPrivateNetworksAreEnabled()
    {
        var result = await Validator(allowPrivateNetworks: true, resolvesTo: "127.0.0.1")
            .ValidateAsync("http://localhost:5222/callback");

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task RejectsHostnameThatResolvesToBothPublicAndPrivateAddresses()
    {
        var result = await Validator(resolvesTo: ["93.184.216.34", "10.0.0.5"])
            .ValidateAsync("https://example.com/callback");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ExplainsWhyUrlWasRejected()
    {
        var result = await Validator(resolvesTo: "10.0.0.5").ValidateAsync("http://10.0.0.5/callback");

        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }
}
