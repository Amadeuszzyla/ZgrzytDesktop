using System;
using System.Text.Json.Serialization;

namespace ZgrzytDesktop.Models;

public class AuditLogEntry
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [JsonPropertyName("user_login")]
    public string UserLogin { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("ticket_id")]
    public int? TicketId { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
