using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.Models;

public class Ticket
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonIgnore]
    public string DisplayStatus => StatusDisplayHelper.ToDisplayStatus(Status);

    [JsonIgnore]
    public string DisplayCategory => TicketCategoryHelper.ExtractCategory(Title, Description);

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("assigned_it_id")]
    public int? AssignedItId { get; set; }

    [JsonPropertyName("user")]
    public User? User { get; set; }

    [JsonPropertyName("assignedTo")]
    public User? AssignedTo { get; set; }

    [JsonPropertyName("assigned_to")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    public User? AssignedToLegacy
    {
        get => null;
        set
        {
            if (AssignedTo is null)
                AssignedTo = value;
        }
    }

    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; } = new();

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("closed_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    public DateTime? ClosedAt { get; set; }

    [JsonPropertyName("category")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    public string? Category { get; set; }
}

/// <summary>
/// Rozszerzony widok zgłoszenia z API (pełniejszy kontrakt niż lista).
/// </summary>
public class TicketFull : Ticket
{
    [JsonPropertyName("resolved_at")]
    public DateTime? ResolvedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}