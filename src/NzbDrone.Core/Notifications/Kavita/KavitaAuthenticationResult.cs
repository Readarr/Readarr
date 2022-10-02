using System.Text.Json.Serialization;

namespace NzbDrone.Core.Notifications.Kavita;

public class KavitaAuthenticationResult
{
    [JsonPropertyName("token")]
    public string Token { get; set; }
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; init; }
}
