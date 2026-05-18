using System;
using System.Text.Json.Serialization;
using ZgrzytDesktop.Converters;

namespace ZgrzytDesktop.Models;

public class User
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("active")]
    [JsonConverter(typeof(FlexibleBoolJsonConverter))]
    public bool Active { get; set; }

    [JsonPropertyName("ban")]
    [JsonConverter(typeof(FlexibleBoolJsonConverter))]
    public bool Ban { get; set; }

    [JsonPropertyName("banned_at")]
    public DateTime? BannedAt { get; set; }

    [JsonPropertyName("activated_at")]
    public DateTime? ActivatedAt { get; set; }

    [JsonPropertyName("activated_by")]
    public int? ActivatedBy { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}