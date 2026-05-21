using System.Net;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;
using TicketMessage = ZgrzytDesktop.Models.Message;

namespace ZgrzytDesktop.Tests.Infrastructure.Fakes;

public sealed class FakeTicketService : ITicketService
{
    public TicketQueueView? LastQueueView { get; private set; }

    public int? LastPage { get; private set; }

    public Exception? GetTicketsException { get; set; }

    public ApiException? GetTicketsApiException { get; set; }

    public Dictionary<int, PaginatedResponse<Ticket>> PagedResponses { get; } = new();

    public int GetTicketsCallCount { get; private set; }

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
        TicketQueueView queueView = TicketQueueView.All)
    {
        GetTicketsCallCount++;
        LastQueueView = queueView;
        LastPage = page;

        if (GetTicketsException is not null)
            throw GetTicketsException;

        if (GetTicketsApiException is not null)
            throw GetTicketsApiException;

        if (PagedResponses.TryGetValue(page, out var paged))
            return Task.FromResult<PaginatedResponse<Ticket>?>(paged);

        return Task.FromResult(NextTicketsResponse);
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

    public Task<Ticket?> GetTicketAsync(int id) => Task.FromResult<Ticket?>(null);

    public Task<List<TicketMessage>> GetTicketMessagesAsync(int ticketId) =>
        Task.FromResult(new List<TicketMessage>());

    public Task<Ticket?> CreateTicketAsync(CreateTicketRequest request) =>
        Task.FromResult<Ticket?>(null);

    public Task<Ticket?> UpdateTicketAsync(int id, UpdateTicketRequest request) =>
        Task.FromResult<Ticket?>(null);

    public Task<TicketMessage?> SendMessageAsync(int ticketId, string body) =>
        Task.FromResult<TicketMessage?>(null);

    public Task<bool> DeleteTicketAsync(int id) => Task.FromResult(true);
}
