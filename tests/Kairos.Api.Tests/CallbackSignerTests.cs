using Kairos.Api.Services;

namespace Kairos.Api.Tests;

public class CallbackSignerTests
{
    private const string Payload = """{"uniqueId":"order-42"}""";

    [Fact]
    public void MatchesAnIndependentlyComputedHmac()
    {
        // openssl dgst -sha256 -hmac 'topsecret'
        Assert.Equal(
            "sha256=ad71a7ebe1b04e94f88325a62960914c67e09c7d70a778768dce84523d693737",
            CallbackSigner.Sign(Payload, "topsecret"));
    }

    [Fact]
    public void ProducesADifferentSignatureForADifferentSecret()
    {
        Assert.NotEqual(CallbackSigner.Sign(Payload, "topsecret"), CallbackSigner.Sign(Payload, "other"));
    }

    [Fact]
    public void ProducesADifferentSignatureForADifferentPayload()
    {
        Assert.NotEqual(CallbackSigner.Sign(Payload, "topsecret"), CallbackSigner.Sign("{}", "topsecret"));
    }
}
