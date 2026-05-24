using System.Collections.Generic;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.Services.Interfaces;

public interface ITicketService
{
    Task<PaginatedResponse<Ticket>?> GetTicketsAsync(
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
        int currentUserId = 0);

    Task<PaginatedResponse<Ticket>?> GetActiveTicketsAsync(
        int page = 1,
        int perPage = 15,
        string? search = null);

    Task<PaginatedResponse<Ticket>?> GetUnassignedTicketsAsync(
        int page = 1,
        int perPage = 15,
        string? search = null);

    Task<Ticket?> GetTicketAsync(int id);

    Task<List<Message>> GetTicketMessagesAsync(int ticketId);

    Task<Ticket?> CreateTicketAsync(CreateTicketRequest request);

    Task<Ticket?> UpdateTicketAsync(int id, UpdateTicketRequest request);

    Task<Message?> SendMessageAsync(int ticketId, string body);

    Task<bool> DeleteTicketAsync(int id);
}
