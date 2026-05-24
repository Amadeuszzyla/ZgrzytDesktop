using System.Net;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;
using TicketMessage = ZgrzytDesktop.Models.Message;

namespace ZgrzytDesktop.Tests.Infrastructure.Fakes;

public sealed class FakeTicketService : ITicketService
{
    public TicketQueueView? LastQueueView { get; private set; }

    public int? LastPage { get; private set; }

    public int? LastPerPage { get; private set; }

    public string? LastSearch { get; private set; }

    public string? LastStatus { get; private set; }

    public string? LastPriority { get; private set; }

    public string? LastSortBy { get; private set; }

    public string? LastSortDirection { get; private set; }

    public string? LastCategoryFilter { get; private set; }

    public string? LastAssignmentFilter { get; private set; }

    public int LastCurrentUserId { get; private set; }

    public Exception? GetTicketsException { get; set; }

    public ApiException? GetTicketsApiException { get; set; }

    public Dictionary<int, PaginatedResponse<Ticket>> PagedResponses { get; } = new();

    public int GetTicketsCallCount { get; private set; }

    public CreateTicketRequest? LastCreateRequest { get; private set; }

    public Ticket? NextCreatedTicket { get; set; }

    public ApiException? CreateTicketApiException { get; set; }

    public int CreateTicketCallCount { get; private set; }

    public PaginatedResponse<Ticket>? NextTicketsResponse { get; set; } =
        new()
        {
            Data = new List<Ticket>(),
            Total = 0,
            LastPage = 1,
            CurrentPage = 1
        };

    public Task<PaginatedResponse<Ticket>?> GetTicketsAsync(
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
        GetTicketsCallCount++;
        LastQueueView = queueView;
        LastPage = page;
        LastPerPage = perPage;
        LastSearch = search;
        LastStatus = status;
        LastPriority = priority;
        LastSortBy = sortBy;
        LastSortDirection = sortDirection;
        LastCategoryFilter = categoryFilter;
        LastAssignmentFilter = assignmentFilter;
        LastCurrentUserId = currentUserId;

        if (GetTicketsException is not null)
            throw GetTicketsException;

        if (GetTicketsApiException is not null)
            throw GetTicketsApiException;

        if (queueView != TicketQueueView.All)
        {
            var source = NextTicketsResponse?.Data ?? [];

            if (!TicketQueueListProcessor.RequiresLocalProcessing(
                    status, priority, assignmentFilter, categoryFilter, sortBy, sortDirection))
            {
                var serverFiltered = TicketQueueListProcessor.Filter(
                    source,
                    status: null,
                    priority: null,
                    search,
                    categoryFilter,
                    assignmentFilter,
                    currentUserId);
                return Task.FromResult<PaginatedResponse<Ticket>?>(
                    TicketQueueListProcessor.Paginate(serverFiltered, page, perPage));
            }

            var filtered = TicketQueueListProcessor.Filter(
                source, status, priority, search, categoryFilter, assignmentFilter, currentUserId);
            var sorted = TicketQueueListProcessor.Sort(filtered, sortBy, sortDirection);
            return Task.FromResult<PaginatedResponse<Ticket>?>(
                TicketQueueListProcessor.Paginate(sorted, page, perPage));
        }

        if (PagedResponses.TryGetValue(page, out var paged))
            return Task.FromResult(ApplyCategoryFilter(paged, categoryFilter));

        return Task.FromResult(ApplyCategoryFilter(NextTicketsResponse, categoryFilter));
    }

    private static PaginatedResponse<Ticket>? ApplyCategoryFilter(
        PaginatedResponse<Ticket>? response,
        string? categoryFilter)
    {
        if (response?.Data is null || TicketCategoryFilterKeys.IsAll(categoryFilter))
            return response;

        var filtered = response.Data
            .Where(ticket => TicketCategoryFilter.Matches(ticket, categoryFilter))
            .ToList();

        return new PaginatedResponse<Ticket>
        {
            Data = filtered,
            Total = filtered.Count,
            CurrentPage = response.CurrentPage,
            LastPage = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)Math.Max(1, response.PerPage))),
            PerPage = response.PerPage
        };
    }

    public Task<PaginatedResponse<Ticket>?> GetActiveTicketsAsync(
        int page = 1,
        int perPage = 15,
        string? search = null) =>
        GetTicketsAsync(page, perPage, search, queueView: TicketQueueView.Active);

    public Task<PaginatedResponse<Ticket>?> GetUnassignedTicketsAsync(
        int page = 1,
        int perPage = 15,
        string? search = null) =>
        GetTicketsAsync(page, perPage, search, queueView: TicketQueueView.Unassigned);

    public Dictionary<int, Ticket> TicketsById { get; } = new();

    public ApiException? GetTicketApiException { get; set; }

    public Dictionary<int, List<TicketMessage>> MessagesByTicketId { get; } = new();

    public int GetTicketCallCount { get; private set; }

    public int GetTicketMessagesCallCount { get; private set; }

    public Task<Ticket?> GetTicketAsync(int id)
    {
        GetTicketCallCount++;

        if (GetTicketApiException is not null)
            throw GetTicketApiException;

        return Task.FromResult(TicketsById.TryGetValue(id, out var ticket) ? ticket : null);
    }

    public Task<List<TicketMessage>> GetTicketMessagesAsync(int ticketId)
    {
        GetTicketMessagesCallCount++;

        if (MessagesByTicketId.TryGetValue(ticketId, out var messages))
            return Task.FromResult(messages);

        return Task.FromResult(new List<TicketMessage>());
    }

    public Task<Ticket?> CreateTicketAsync(CreateTicketRequest request)
    {
        CreateTicketCallCount++;
        LastCreateRequest = request;

        if (CreateTicketApiException is not null)
            throw CreateTicketApiException;

        return Task.FromResult(NextCreatedTicket);
    }

    public int? LastUpdateTicketId { get; private set; }

    public UpdateTicketRequest? LastUpdateRequest { get; private set; }

    public Ticket? NextUpdatedTicket { get; set; }

    public ApiException? UpdateTicketApiException { get; set; }

    public ApiException? SendMessageApiException { get; set; }

    public ApiException? DeleteTicketApiException { get; set; }

    public bool DeleteTicketResult { get; set; } = true;

    public string? LastSendMessageBody { get; private set; }

    public int UpdateTicketCallCount { get; private set; }

    public int SendMessageCallCount { get; private set; }

    public int DeleteTicketCallCount { get; private set; }

    public Task<Ticket?> UpdateTicketAsync(int id, UpdateTicketRequest request)
    {
        UpdateTicketCallCount++;
        LastUpdateTicketId = id;
        LastUpdateRequest = request;

        if (UpdateTicketApiException is not null)
            throw UpdateTicketApiException;

        if (NextUpdatedTicket is not null)
            return Task.FromResult<Ticket?>(NextUpdatedTicket);

        if (TicketsById.TryGetValue(id, out var existing))
        {
            if (request.AssignedItId.HasValue)
                existing.AssignedItId = request.AssignedItId;

            if (request.Status is not null)
                existing.Status = request.Status;

            if (request.Priority is not null)
                existing.Priority = request.Priority;

            return Task.FromResult<Ticket?>(existing);
        }

        return Task.FromResult<Ticket?>(null);
    }

    public Task<TicketMessage?> SendMessageAsync(int ticketId, string body)
    {
        SendMessageCallCount++;
        LastSendMessageBody = body;

        if (SendMessageApiException is not null)
            throw SendMessageApiException;

        var message = new TicketMessage { Id = SendMessageCallCount, Content = body, TicketId = ticketId };

        if (!MessagesByTicketId.TryGetValue(ticketId, out var list))
        {
            list = new List<TicketMessage>();
            MessagesByTicketId[ticketId] = list;
        }

        list.Add(message);
        return Task.FromResult<TicketMessage?>(message);
    }

    public Task<bool> DeleteTicketAsync(int id)
    {
        DeleteTicketCallCount++;

        if (DeleteTicketApiException is not null)
            throw DeleteTicketApiException;

        TicketsById.Remove(id);
        return Task.FromResult(DeleteTicketResult);
    }
}
