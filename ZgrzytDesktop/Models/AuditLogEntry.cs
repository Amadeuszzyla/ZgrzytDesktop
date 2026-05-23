using System;
using System.Text.Json.Serialization;
using ZgrzytDesktop.Helpers;

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

    [JsonPropertyName("details_key")]
    public string? DetailsKey { get; set; }

    [JsonPropertyName("parameters_json")]
    public string? ParametersJson { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonIgnore]
    public string DisplayAction => AuditDisplayHelper.GetActionDisplay(Action);

    [JsonIgnore]
    public string DisplayDescription => AuditDisplayHelper.GetDescriptionDisplay(this);

    [JsonIgnore]
    public string DisplayUserLogin => AuditDisplayHelper.FormatUserLogin(UserLogin);

    [JsonIgnore]
    public string DisplayTicketLabel => AuditDisplayHelper.FormatTicketId(TicketId);
}
