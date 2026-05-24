using System;
using System.Collections.Generic;
using System.Globalization;
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

    private static Dictionary<string, string>? _displayLabelToApiStatus;

    public static string NormalizeApiStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        foreach (var apiStatus in ApiToResourceKey.Keys)
        {
            if (string.Equals(apiStatus, trimmed, StringComparison.OrdinalIgnoreCase))
                return apiStatus;
        }

        return trimmed.ToLowerInvariant() switch
        {
            "new" => TicketStatuses.Nowe,
            "in progress" or "in_progress" => TicketStatuses.WTrakcie,
            "closed" => TicketStatuses.Zamkniete,
            _ => trimmed
        };
    }

    public static string ToDisplayStatus(string? apiOrDisplayStatus)
    {
        if (string.IsNullOrWhiteSpace(apiOrDisplayStatus))
            return string.Empty;

        var apiStatus = NormalizeApiStatus(apiOrDisplayStatus);
        if (ApiToResourceKey.TryGetValue(apiStatus, out var key))
            return AppStrings.Get(key);

        if (TryResolveApiFromDisplayLabel(apiOrDisplayStatus.Trim(), out var apiFromLabel))
            return ToDisplayStatus(apiFromLabel);

        return apiOrDisplayStatus.Trim();
    }

    public static string ToApiStatus(string? displayOrApiStatus)
    {
        if (string.IsNullOrWhiteSpace(displayOrApiStatus))
            return TicketStatuses.Nowe;

        var normalized = displayOrApiStatus.Trim();
        var apiFromNormalized = NormalizeApiStatus(normalized);
        if (ApiToResourceKey.ContainsKey(apiFromNormalized))
            return apiFromNormalized;

        if (TryResolveApiFromDisplayLabel(normalized, out var apiFromLabel))
            return apiFromLabel;

        return TicketStatuses.Nowe;
    }

    public static bool IsKnownDisplayStatus(string? displayOrApiStatus)
    {
        if (string.IsNullOrWhiteSpace(displayOrApiStatus))
            return false;

        var trimmed = displayOrApiStatus.Trim();
        if (ApiToResourceKey.ContainsKey(NormalizeApiStatus(trimmed)))
            return true;

        return TryResolveApiFromDisplayLabel(trimmed, out _);
    }

    public static string GetStatusBadgeClasses(string? apiOrDisplayStatus)
    {
        var apiStatus = NormalizeApiStatus(apiOrDisplayStatus);
        if (string.IsNullOrWhiteSpace(apiStatus))
            return "ticket-badge ticket-badge-status-default";

        if (string.Equals(apiStatus, TicketStatuses.Nowe, StringComparison.OrdinalIgnoreCase))
            return "ticket-badge ticket-badge-status-new";

        if (string.Equals(apiStatus, TicketStatuses.WTrakcie, StringComparison.OrdinalIgnoreCase))
            return "ticket-badge ticket-badge-status-progress";

        if (string.Equals(apiStatus, TicketStatuses.Zamkniete, StringComparison.OrdinalIgnoreCase))
            return "ticket-badge ticket-badge-status-closed";

        return "ticket-badge ticket-badge-status-default";
    }

    private static bool TryResolveApiFromDisplayLabel(string label, out string apiStatus)
    {
        EnsureDisplayLabelMap();
        if (_displayLabelToApiStatus!.TryGetValue(label, out apiStatus!))
            return true;

        apiStatus = TicketStatuses.Nowe;
        return false;
    }

    private static void EnsureDisplayLabelMap()
    {
        if (_displayLabelToApiStatus is not null)
            return;

        var previousCulture = CultureInfo.CurrentUICulture.Name;
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var culture in new[] { "pl", "en" })
        {
            AppStrings.ApplyCulture(culture);
            foreach (var pair in ApiToResourceKey)
                map[AppStrings.Get(pair.Value)] = pair.Key;
        }

        AppStrings.ApplyCulture(previousCulture.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en" : "pl");
        _displayLabelToApiStatus = map;
    }
}
