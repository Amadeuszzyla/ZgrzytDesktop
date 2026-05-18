using System;
using System.Text.Json.Serialization;

namespace ZgrzytDesktop.Models;

public class Message
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("message")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("ticket_id")]
    public int TicketId { get; set; }

    [JsonPropertyName("sender_id")]
    public int SenderId { get; set; }

    [JsonPropertyName("sender")]
    public User? Sender { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}