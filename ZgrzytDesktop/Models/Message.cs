using System;
using System.Text.Json.Serialization;

namespace ZgrzytDesktop.Models;

public class Message
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("body")]
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

/// <summary>
/// Rozszerzony model wiadomości (dodatkowe pola z API, jeśli dostępne).
/// </summary>
public class MessageFull : Message
{
    [JsonPropertyName("read_at")]
    public DateTime? ReadAt { get; set; }

    [JsonPropertyName("is_internal")]
    public bool? IsInternal { get; set; }
}