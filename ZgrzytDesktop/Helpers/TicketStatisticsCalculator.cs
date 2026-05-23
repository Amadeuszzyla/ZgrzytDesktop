using System;
using System.Collections.Generic;
using System.Linq;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Helpers;

public sealed class TicketStatisticsSnapshot
{
    public int Total { get; init; }

    public int New { get; init; }

    public int InProgress { get; init; }

    public int Closed { get; init; }

    public int LowPriority { get; init; }

    public int MediumPriority { get; init; }

    public int HighPriority { get; init; }

    public int Assigned { get; init; }

    public int Unassigned { get; init; }
}

public static class TicketStatisticsCalculator
{
    public static TicketStatisticsSnapshot Compute(IReadOnlyList<Ticket> tickets)
    {
        var assigned = tickets.Count(ticket => ticket.AssignedItId.HasValue);

        return new TicketStatisticsSnapshot
        {
            Total = tickets.Count,
            New = CountByStatus(tickets, TicketStatuses.Nowe),
            InProgress = CountByStatus(tickets, TicketStatuses.WTrakcie),
            Closed = CountByStatus(tickets, TicketStatuses.Zamkniete),
            LowPriority = CountByPriority(tickets, TicketPriorities.Low),
            MediumPriority = CountByPriority(tickets, TicketPriorities.Medium),
            HighPriority = CountByPriority(tickets, TicketPriorities.High),
            Assigned = assigned,
            Unassigned = Math.Max(0, tickets.Count - assigned)
        };
    }

    private static int CountByStatus(IReadOnlyList<Ticket> tickets, string status) =>
        tickets.Count(ticket => string.Equals(ticket.Status, status, StringComparison.OrdinalIgnoreCase));

    private static int CountByPriority(IReadOnlyList<Ticket> tickets, string priority) =>
        tickets.Count(ticket => string.Equals(ticket.Priority, priority, StringComparison.OrdinalIgnoreCase));
}
