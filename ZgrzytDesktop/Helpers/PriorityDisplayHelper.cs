using System;
using System.Collections.Generic;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Helpers;

public static class PriorityDisplayHelper
{
    private static readonly Dictionary<string, string> ApiToResourceKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [TicketPriorities.Low] = "Priority_Low",
            [TicketPriorities.Medium] = "Priority_Medium",
            [TicketPriorities.High] = "Priority_High"
        };

    public static string ToDisplayPriority(string? apiPriority)
    {
        if (string.IsNullOrWhiteSpace(apiPriority))
            return string.Empty;

        var normalized = apiPriority.Trim();

        if (ApiToResourceKey.TryGetValue(normalized, out var key))
            return AppStrings.Get(key);

        if (IsKnownDisplayLabel(normalized, out var apiFromDisplay))
            return ToDisplayPriority(apiFromDisplay);

        return normalized;
    }

    public static string ToApiPriority(string? displayOrApiPriority)
    {
        if (string.IsNullOrWhiteSpace(displayOrApiPriority))
            return TicketPriorities.Low;

        var normalized = displayOrApiPriority.Trim();

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
            var value when string.Equals(value, TicketPriorities.Low, StringComparison.OrdinalIgnoreCase) =>
                TicketPriorities.Low,
            var value when string.Equals(value, TicketPriorities.Medium, StringComparison.OrdinalIgnoreCase) =>
                TicketPriorities.Medium,
            var value when string.Equals(value, TicketPriorities.High, StringComparison.OrdinalIgnoreCase) =>
                TicketPriorities.High,
            _ => TicketPriorities.Low
        };
    }

    private static bool IsKnownDisplayLabel(string value, out string apiPriority)
    {
        foreach (var pair in ApiToResourceKey)
        {
            if (string.Equals(AppStrings.Get(pair.Value), value, StringComparison.OrdinalIgnoreCase))
            {
                apiPriority = pair.Key;
                return true;
            }
        }

        apiPriority = TicketPriorities.Low;
        return false;
    }
}
