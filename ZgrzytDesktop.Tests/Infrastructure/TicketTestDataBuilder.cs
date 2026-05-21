using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.Tests.Infrastructure;

internal static class TicketTestDataBuilder
{
    public static List<Ticket> CreateMixedStatisticsSet() =>
    [
        Create(1, TicketStatuses.Nowe, TicketPriorities.Low, assigned: false),
        Create(2, TicketStatuses.Nowe, TicketPriorities.Medium, assigned: true, assigneeId: 5),
        Create(3, TicketStatuses.WTrakcie, TicketPriorities.High, assigned: true, assigneeId: 6),
        Create(4, TicketStatuses.Zamkniete, TicketPriorities.Low, assigned: false),
        Create(5, TicketStatuses.Zamkniete, TicketPriorities.Medium, assigned: true, assigneeId: 7)
    ];

    public static PaginatedResponse<Ticket> CreatePage(
        int page,
        IReadOnlyList<Ticket> tickets,
        int total,
        int lastPage) =>
        new()
        {
            Data = tickets.ToList(),
            Total = total,
            LastPage = lastPage,
            CurrentPage = page
        };

    public static Ticket Create(
        int id,
        string status,
        string priority,
        bool assigned,
        int? assigneeId = null) =>
        new()
        {
            Id = id,
            Title = $"Ticket {id}",
            Description = "Test",
            Status = status,
            Priority = priority,
            AssignedItId = assigned ? assigneeId : null
        };
}
