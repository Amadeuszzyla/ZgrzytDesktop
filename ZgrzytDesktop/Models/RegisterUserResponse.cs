using System.Text.Json.Serialization;

namespace ZgrzytDesktop.Models;

public class RegisterUserResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("user")]
    public User? User { get; set; }
}
