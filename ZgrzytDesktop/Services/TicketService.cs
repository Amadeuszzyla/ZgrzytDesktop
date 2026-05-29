using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Services;

public enum TicketQueueView
{
    All,
    Active,
    Unassigned
}

public class TicketService : ITicketService
{
    private readonly IApiService _apiService;

    public TicketService(IApiService apiService)
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
        string sortDirection = "desc",
        TicketQueueView queueView = TicketQueueView.All,
        string? categoryFilter = null,
        string? assignmentFilter = null,
        int currentUserId = 0)
    {
        var endpointName = ResolveEndpointName(queueView);

        if (queueView == TicketQueueView.All)
        {
            var response = await FetchTicketsPageAsync(
                endpointName,
                page,
                perPage,
                search,
                status,
                priority,
                sortBy,
                sortDirection,
                includeFilterAndSortParams: true);

            return ApplyClientSideFilters(
                response,
                status,
                priority,
                categoryFilter,
                assignmentFilter,
                currentUserId);
        }

        if (!TicketQueueListProcessor.RequiresLocalProcessing(
                status, priority, assignmentFilter, categoryFilter, sortBy, sortDirection))
        {
            var response = await FetchTicketsPageAsync(
                endpointName,
                page,
                perPage,
                search,
                status: null,
                priority: null,
                sortBy,
                sortDirection,
                includeFilterAndSortParams: false);

            return ApplyClientSideFilters(
                response,
                status,
                priority,
                categoryFilter,
                assignmentFilter,
                currentUserId);
        }

        var aggregation = await FetchAllQueuePagesAsync(endpointName, search);
        var filtered = TicketQueueListProcessor.Filter(
            aggregation.Tickets,
            status,
            priority,
            search,
            categoryFilter,
            assignmentFilter,
            currentUserId);
        var sorted = TicketQueueListProcessor.Sort(filtered, sortBy, sortDirection);
        var paginated = TicketQueueListProcessor.Paginate(sorted, page, perPage);
        paginated.IsQueueFetchTruncated = aggregation.Truncated;
        paginated.QueuePagesFetched = aggregation.PagesFetched;
        paginated.QueueApiReportedTotal = aggregation.ApiReportedTotal;
        return paginated;
    }

    public async Task<PaginatedResponse<Ticket>?> GetActiveTicketsAsync(
        int page = 1,
        int perPage = 15,
        string? search = null)
    {
        return await GetTicketsAsync(
            page: page,
            perPage: perPage,
            search: search,
            queueView: TicketQueueView.Active);
    }

    public async Task<PaginatedResponse<Ticket>?> GetUnassignedTicketsAsync(
        int page = 1,
        int perPage = 15,
        string? search = null)
    {
        return await GetTicketsAsync(
            page: page,
            perPage: perPage,
            search: search,
            queueView: TicketQueueView.Unassigned);
    }

    public async Task<Ticket?> GetTicketAsync(int id)
    {
        return await _apiService.GetAsync<TicketFull>($"tickets/{id}");
    }

    public async Task<List<Message>> GetTicketMessagesAsync(int ticketId)
    {
        var messages = await _apiService.GetAsync<List<MessageFull>>(
            $"tickets/{ticketId}/messages"
        );

        if (messages is null)
            return new List<Message>();

        return messages.Select(message => (Message)message).ToList();
    }

    public async Task<Ticket?> CreateTicketAsync(CreateTicketRequest request)
    {
        return await _apiService.PostAsync<CreateTicketRequest, Ticket>("tickets", request);
    }

    public async Task<Ticket?> UpdateTicketAsync(int id, UpdateTicketRequest request)
    {
        return await _apiService.PutAsync<UpdateTicketRequest, Ticket>($"tickets/{id}", request);
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

    private static string ResolveEndpointName(TicketQueueView queueView) =>
        queueView switch
        {
            TicketQueueView.Active => "active-tickets",
            TicketQueueView.Unassigned => "unassigned-tickets",
            _ => "tickets"
        };

    private async Task<PaginatedResponse<Ticket>?> FetchTicketsPageAsync(
        string endpointName,
        int page,
        int perPage,
        string? search,
        string? status,
        string? priority,
        string sortBy,
        string sortDirection,
        bool includeFilterAndSortParams)
    {
        var queryParameters = new List<string>
        {
            $"page={page}",
            $"per_page={perPage}"
        };

        AddQueryParameter(queryParameters, "search", search);

        if (includeFilterAndSortParams)
        {
            AddQueryParameter(queryParameters, "status", status);
            AddQueryParameter(queryParameters, "priority", priority);
            AddQueryParameter(queryParameters, "sort_by", sortBy);
            AddQueryParameter(queryParameters, "sort_direction", sortDirection);
        }

        var endpoint = $"{endpointName}?{string.Join("&", queryParameters)}";
        return await _apiService.GetAsync<PaginatedResponse<Ticket>>(endpoint);
    }

    private Task<QueuePageAggregationResult> FetchAllQueuePagesAsync(string endpointName, string? search) =>
        TicketQueuePageAggregator.FetchAllPagesAsync(
            (fetchPage, pageSize) => FetchTicketsPageAsync(
                endpointName,
                fetchPage,
                pageSize,
                search,
                status: null,
                priority: null,
                sortBy: TicketSortHelper.DefaultField.SortBy,
                sortDirection: TicketSortHelper.DefaultDirection.Direction,
                includeFilterAndSortParams: false));

    private static void AddQueryParameter(List<string> parameters, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        parameters.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
    }

    private static PaginatedResponse<Ticket>? ApplyClientSideFilters(
        PaginatedResponse<Ticket>? response,
        string? status,
        string? priority,
        string? categoryFilter,
        string? assignmentFilter,
        int currentUserId)
    {
        if (response?.Data is null)
            return response;

        if (string.IsNullOrWhiteSpace(status) &&
            string.IsNullOrWhiteSpace(priority) &&
            TicketCategoryFilterKeys.IsAll(categoryFilter) &&
            TicketAssignmentFilterKeys.IsAll(assignmentFilter))
        {
            return response;
        }

        var filtered = TicketQueueListProcessor.Filter(
            response.Data,
            status,
            priority,
            search: null,
            categoryFilter,
            assignmentFilter,
            currentUserId);

        return new PaginatedResponse<Ticket>
        {
            Data = filtered,
            Total = filtered.Count,
            CurrentPage = response.CurrentPage,
            LastPage = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)Math.Max(1, response.PerPage))),
            PerPage = response.PerPage,
            IsQueueFetchTruncated = response.IsQueueFetchTruncated,
            QueuePagesFetched = response.QueuePagesFetched,
            QueueApiReportedTotal = response.QueueApiReportedTotal
        };
    }
}

public class CreateTicketRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = TicketPriorities.Low;
}

public class UpdateTicketRequest
{
    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; set; }

    [JsonPropertyName("priority")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Priority { get; set; }

    [JsonPropertyName("assigned_it_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? AssignedItId { get; set; }
}

public class CreateMessageRequest
{
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}
