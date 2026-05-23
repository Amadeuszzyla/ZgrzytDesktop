using System;
using System.Collections.Generic;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Helpers;

public static class StatusDisplayHelper
{
    private static readonly Dictionary<string, string> ApiToResourceKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [TicketStatuses.Nowe] = "Status_New",
            [TicketStatuses.WTrakcie] = "Status_InProgress",
            [TicketStatuses.Zamkniete] = "Status_Closed"
        };

    public static string ToDisplayStatus(string? apiStatus)
    {
        if (string.IsNullOrWhiteSpace(apiStatus))
            return string.Empty;

        var normalized = apiStatus.Trim();

        if (ApiToResourceKey.TryGetValue(normalized, out var key))
            return AppStrings.Get(key);

        if (IsKnownDisplayLabel(normalized, out var apiFromDisplay))
            return ToDisplayStatus(apiFromDisplay);

        return normalized;
    }

    public static string ToApiStatus(string? displayStatus)
    {
        if (string.IsNullOrWhiteSpace(displayStatus))
            return TicketStatuses.Nowe;

        var normalized = displayStatus.Trim();

        foreach (var pair in ApiToResourceKey)
        {
            if (string.Equals(pair.Key, normalized, StringComparison.OrdinalIgnoreCase))
                return pair.Key;
        }

        foreach (var pair in ApiToResourceKey)
        {
            if (string.Equals(AppStrings.Get(pair.Value), normalized, StringComparison.OrdinalIgnoreCase))
                return pair.Key;
        }

        return normalized switch
        {
            var value when string.Equals(value, TicketStatuses.Nowe, StringComparison.OrdinalIgnoreCase) =>
                TicketStatuses.Nowe,
            var value when string.Equals(value, TicketStatuses.WTrakcie, StringComparison.OrdinalIgnoreCase) =>
                TicketStatuses.WTrakcie,
            var value when string.Equals(value, TicketStatuses.Zamkniete, StringComparison.OrdinalIgnoreCase) =>
                TicketStatuses.Zamkniete,
            _ => TicketStatuses.Nowe
        };
    }

    private static bool IsKnownDisplayLabel(string value, out string apiStatus)
    {
        foreach (var pair in ApiToResourceKey)
        {
            if (string.Equals(AppStrings.Get(pair.Value), value, StringComparison.OrdinalIgnoreCase))
            {
                apiStatus = pair.Key;
                return true;
            }
        }

        apiStatus = TicketStatuses.Nowe;
        return false;
    }
}
