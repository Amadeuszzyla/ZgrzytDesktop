using System;
using System.Collections.Generic;
using ZgrzytDesktop.Constants;

namespace ZgrzytDesktop.Helpers;

public static class StatusDisplayHelper
{
    private static readonly Dictionary<string, string> ApiToDisplay =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [TicketStatuses.Nowe] = TicketStatuses.DisplayNowe,
            [TicketStatuses.WTrakcie] = TicketStatuses.DisplayWToku,
            [TicketStatuses.Zamkniete] = TicketStatuses.DisplayRozwiazane
        };

    public static string ToDisplayStatus(string? apiStatus)
    {
        if (string.IsNullOrWhiteSpace(apiStatus))
            return string.Empty;

        var normalized = apiStatus.Trim();

        return ApiToDisplay.TryGetValue(normalized, out var display)
            ? display
            : normalized;
    }

    public static string ToApiStatus(string? displayStatus)
    {
        if (string.IsNullOrWhiteSpace(displayStatus))
            return TicketStatuses.Nowe;

        return displayStatus.Trim() switch
        {
            TicketStatuses.DisplayNowe => TicketStatuses.Nowe,
            TicketStatuses.DisplayWToku => TicketStatuses.WTrakcie,
            TicketStatuses.DisplayRozwiazane => TicketStatuses.Zamkniete,
            TicketStatuses.Nowe => TicketStatuses.Nowe,
            TicketStatuses.WTrakcie => TicketStatuses.WTrakcie,
            TicketStatuses.Zamkniete => TicketStatuses.Zamkniete,
            _ => TicketStatuses.Nowe
        };
    }
}
