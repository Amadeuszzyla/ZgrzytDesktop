using System;
using System.Collections.Generic;
using System.Globalization;
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

    private static Dictionary<string, string>? _displayLabelToApiPriority;

    public static string NormalizeApiPriority(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        foreach (var apiPriority in ApiToResourceKey.Keys)
        {
            if (string.Equals(apiPriority, trimmed, StringComparison.OrdinalIgnoreCase))
                return apiPriority;
        }

        return trimmed.ToLowerInvariant() switch
        {
            "low" => TicketPriorities.Low,
            "medium" => TicketPriorities.Medium,
            "high" => TicketPriorities.High,
            _ => trimmed
        };
    }

    public static string ToDisplayPriority(string? apiOrDisplayPriority)
    {
        if (string.IsNullOrWhiteSpace(apiOrDisplayPriority))
            return string.Empty;

        var apiPriority = NormalizeApiPriority(apiOrDisplayPriority);
        if (ApiToResourceKey.TryGetValue(apiPriority, out var key))
            return AppStrings.Get(key);

        if (TryResolveApiFromDisplayLabel(apiOrDisplayPriority.Trim(), out var apiFromLabel))
            return ToDisplayPriority(apiFromLabel);

        return apiOrDisplayPriority.Trim();
    }

    public static string ToApiPriority(string? displayOrApiPriority)
    {
        if (string.IsNullOrWhiteSpace(displayOrApiPriority))
            return TicketPriorities.Low;

        var normalized = displayOrApiPriority.Trim();
        var apiFromNormalized = NormalizeApiPriority(normalized);
        if (ApiToResourceKey.ContainsKey(apiFromNormalized))
            return apiFromNormalized;

        if (TryResolveApiFromDisplayLabel(normalized, out var apiFromLabel))
            return apiFromLabel;

        return ApiToResourceKey.ContainsKey(apiFromNormalized)
            ? apiFromNormalized
            : TicketPriorities.Low;
    }

    public static bool IsKnownDisplayPriority(string? displayOrApiPriority)
    {
        if (string.IsNullOrWhiteSpace(displayOrApiPriority))
            return false;

        var trimmed = displayOrApiPriority.Trim();
        if (ApiToResourceKey.ContainsKey(NormalizeApiPriority(trimmed)))
            return true;

        return TryResolveApiFromDisplayLabel(trimmed, out _);
    }

    public static string GetPriorityBadgeClasses(string? apiOrDisplayPriority)
    {
        var apiPriority = NormalizeApiPriority(apiOrDisplayPriority);
        if (string.IsNullOrWhiteSpace(apiPriority))
            return "ticket-badge ticket-badge-priority-default";

        if (string.Equals(apiPriority, TicketPriorities.High, StringComparison.OrdinalIgnoreCase))
            return "ticket-badge ticket-badge-priority-high";

        if (string.Equals(apiPriority, TicketPriorities.Medium, StringComparison.OrdinalIgnoreCase))
            return "ticket-badge ticket-badge-priority-medium";

        if (string.Equals(apiPriority, TicketPriorities.Low, StringComparison.OrdinalIgnoreCase))
            return "ticket-badge ticket-badge-priority-low";

        return "ticket-badge ticket-badge-priority-default";
    }

    private static bool TryResolveApiFromDisplayLabel(string label, out string apiPriority)
    {
        EnsureDisplayLabelMap();
        if (_displayLabelToApiPriority!.TryGetValue(label, out apiPriority!))
            return true;

        apiPriority = TicketPriorities.Low;
        return false;
    }

    private static void EnsureDisplayLabelMap()
    {
        if (_displayLabelToApiPriority is not null)
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
        _displayLabelToApiPriority = map;
    }
}
