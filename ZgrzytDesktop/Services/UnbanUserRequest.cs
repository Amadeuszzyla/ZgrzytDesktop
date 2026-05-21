using System.Text.Json.Serialization;

namespace ZgrzytDesktop.Services;

public class UnbanUserRequest
{
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}
