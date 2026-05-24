using System;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Helpers;

public static class TicketAssignmentFilter
{
    public static bool Matches(Ticket ticket, string? filterKey, int currentUserId)
    {
        if (TicketAssignmentFilterKeys.IsAll(filterKey))
            return true;

        var assigneeId = ticket.AssignedItId ?? ticket.AssignedTo?.Id;
        var isAssigned = assigneeId is > 0;

        if (string.Equals(filterKey, TicketAssignmentFilterKeys.Assigned, StringComparison.Ordinal))
            return isAssigned;

        if (string.Equals(filterKey, TicketAssignmentFilterKeys.Unassigned, StringComparison.Ordinal))
            return !isAssigned;

        if (string.Equals(filterKey, TicketAssignmentFilterKeys.AssignedToMe, StringComparison.Ordinal))
            return isAssigned && assigneeId == currentUserId;

        return true;
    }
}
