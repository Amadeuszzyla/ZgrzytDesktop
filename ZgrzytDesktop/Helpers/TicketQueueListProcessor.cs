using System;
using System.Collections.Generic;
using System.Linq;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Helpers;

/// <summary>
/// Client-side filter, sort and pagination for active/unassigned ticket queues
/// when the API list endpoints do not accept the same query params as GET tickets.
/// </summary>
public static class TicketQueueListProcessor
{
    public static bool IsDefaultSort(string sortBy, string sortDirection) =>
        string.Equals(sortBy, TicketSortHelper.DefaultField.SortBy, StringComparison.OrdinalIgnoreCase)
        && string.Equals(sortDirection, TicketSortHelper.DefaultDirection.Direction, StringComparison.OrdinalIgnoreCase);

    public static bool RequiresLocalProcessing(
        string? status,
        string? priority,
        string? assignmentFilter,
        string? categoryFilter,
        string sortBy,
        string sortDirection) =>
        !string.IsNullOrWhiteSpace(status)
        || !string.IsNullOrWhiteSpace(priority)
        || !TicketAssignmentFilterKeys.IsAll(assignmentFilter)
        || !TicketCategoryFilterKeys.IsAll(categoryFilter)
        || !IsDefaultSort(sortBy, sortDirection);

    public static List<Ticket> Filter(
        IReadOnlyList<Ticket> tickets,
        string? status,
        string? priority,
        string? search,
        string? categoryFilter = null,
        string? assignmentFilter = null,
        int currentUserId = 0)
    {
        IEnumerable<Ticket> query = tickets;

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(ticket =>
                string.Equals(ticket.Status, status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(ticket =>
                string.Equals(ticket.Priority, priority, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(ticket =>
                (ticket.Title?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (ticket.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || ticket.Id.ToString().Contains(term, StringComparison.Ordinal));
        }

        if (!TicketCategoryFilterKeys.IsAll(categoryFilter))
        {
            query = query.Where(ticket => TicketCategoryFilter.Matches(ticket, categoryFilter));
        }

        if (!TicketAssignmentFilterKeys.IsAll(assignmentFilter))
        {
            query = query.Where(ticket => TicketAssignmentFilter.Matches(ticket, assignmentFilter, currentUserId));
        }

        return query.ToList();
    }

    public static List<Ticket> Sort(IReadOnlyList<Ticket> tickets, string sortBy, string sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "title" => Order(tickets, ticket => ticket.Title, descending, StringComparer.OrdinalIgnoreCase),
            "status" => Order(tickets, ticket => ticket.Status, descending, StringComparer.OrdinalIgnoreCase),
            "priority" => Order(tickets, ticket => PriorityRank(ticket.Priority), descending, null),
            "updated_at" => Order(tickets, ticket => ticket.UpdatedAt ?? DateTime.MinValue, descending, null),
            _ => Order(tickets, ticket => ticket.CreatedAt ?? DateTime.MinValue, descending, null)
        };
    }

    public static PaginatedResponse<Ticket> Paginate(IReadOnlyList<Ticket> tickets, int page, int perPage)
    {
        var total = tickets.Count;
        var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)perPage));
        var safePage = Math.Clamp(page, 1, lastPage);

        return new PaginatedResponse<Ticket>
        {
            Data = tickets
                .Skip((safePage - 1) * perPage)
                .Take(perPage)
                .ToList(),
            Total = total,
            CurrentPage = safePage,
            LastPage = lastPage,
            PerPage = perPage
        };
    }

    private static List<Ticket> Order<TKey>(
        IReadOnlyList<Ticket> tickets,
        Func<Ticket, TKey> keySelector,
        bool descending,
        IComparer<TKey>? comparer)
    {
        if (descending)
        {
            return comparer is null
                ? tickets.OrderByDescending(keySelector).ToList()
                : tickets.OrderByDescending(keySelector, comparer).ToList();
        }

        return comparer is null
            ? tickets.OrderBy(keySelector).ToList()
            : tickets.OrderBy(keySelector, comparer).ToList();
    }

    private static int PriorityRank(string priority)
    {
        if (string.Equals(priority, TicketPriorities.High, StringComparison.OrdinalIgnoreCase))
            return 3;

        if (string.Equals(priority, TicketPriorities.Medium, StringComparison.OrdinalIgnoreCase))
            return 2;

        if (string.Equals(priority, TicketPriorities.Low, StringComparison.OrdinalIgnoreCase))
            return 1;

        return 0;
    }
}
