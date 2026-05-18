using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Services;

public class TicketService
{
    private readonly ApiService _apiService;

    public TicketService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<PaginatedResponse<Ticket>?> GetTicketsAsync(
        int page = 1,
        int perPage = 15,
        string? search = null,
        string? status = null,
        string? priority = null,
        string sortBy = "created_at",
        string sortDirection = "desc")
    {
        var queryParameters = new List<string>
        {
            $"page={page}",
            $"per_page={perPage}",
            $"sort_by={Uri.EscapeDataString(sortBy)}",
            $"sort_direction={Uri.EscapeDataString(sortDirection)}"
        };

        AddQueryParameter(queryParameters, "search", search);
        AddQueryParameter(queryParameters, "status", status);
        AddQueryParameter(queryParameters, "priority", priority);

        var endpoint = $"tickets?{string.Join("&", queryParameters)}";

        return await _apiService.GetAsync<PaginatedResponse<Ticket>>(endpoint);
    }

    public async Task<Ticket?> GetTicketAsync(int id)
    {
        return await _apiService.GetAsync<Ticket>($"tickets/{id}");
    }

    public async Task<Ticket?> CreateTicketAsync(CreateTicketRequest request)
    {
        return await _apiService.PostAsync<CreateTicketRequest, Ticket>("tickets", request);
    }

    public async Task<Ticket?> UpdateTicketAsync(int id, UpdateTicketRequest request)
    {
        return await _apiService.PatchAsync<UpdateTicketRequest, Ticket>($"tickets/{id}", request);
    }

    public async Task<Message?> SendMessageAsync(int ticketId, string body)
    {
        var request = new CreateMessageRequest
        {
            Body = body
        };

        return await _apiService.PostAsync<CreateMessageRequest, Message>(
            $"tickets/{ticketId}/messages",
            request
        );
    }

    public async Task<bool> DeleteTicketAsync(int id)
    {
        return await _apiService.DeleteAsync($"tickets/{id}");
    }

    private static void AddQueryParameter(List<string> parameters, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        parameters.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
    }
}

public class CreateTicketRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "niski";
}

public class UpdateTicketRequest
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    [JsonPropertyName("assigned_it_id")]
    public int? AssignedItId { get; set; }
}

public class CreateMessageRequest
{
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}