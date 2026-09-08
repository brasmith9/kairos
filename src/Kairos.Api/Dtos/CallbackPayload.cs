namespace Kairos.Api.Dtos;

public class CallbackPayload
{
    public string UniqueId { get; set; } = null!;
    public Dictionary<string, string>? Metadata { get; set; } = null;
}