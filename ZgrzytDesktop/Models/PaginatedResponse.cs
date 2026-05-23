using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ZgrzytDesktop.Models;

public class PaginatedResponse<T>
{
    /// <summary>Set when active/unassigned local fallback stopped at a safety cap.</summary>
    [JsonIgnore]
    public bool IsQueueFetchTruncated { get; set; }

    [JsonIgnore]
    public int QueuePagesFetched { get; set; }

    [JsonIgnore]
    public int? QueueApiReportedTotal { get; set; }
    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();

    [JsonPropertyName("first_page_url")]
    public string? FirstPageUrl { get; set; }

    [JsonPropertyName("from")]
    public int? From { get; set; }

    [JsonPropertyName("last_page")]
    public int LastPage { get; set; }

    [JsonPropertyName("last_page_url")]
    public string? LastPageUrl { get; set; }

    [JsonPropertyName("next_page_url")]
    public string? NextPageUrl { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    [JsonPropertyName("prev_page_url")]
    public string? PrevPageUrl { get; set; }

    [JsonPropertyName("to")]
    public int? To { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}