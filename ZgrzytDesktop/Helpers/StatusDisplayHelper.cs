using System;
using System.Collections.Generic;

namespace ZgrzytDesktop.Helpers;

public static class StatusDisplayHelper
{
    private static readonly Dictionary<string, string> ApiToDisplay =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["nowe"] = "Nowe",
            ["w trakcie"] = "W toku",
            ["zamknięte"] = "Rozwiązane"
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
            return "nowe";

        return displayStatus.Trim() switch
        {
            "Nowe" => "nowe",
            "W toku" => "w trakcie",
            "Rozwiązane" => "zamknięte",
            "nowe" => "nowe",
            "w trakcie" => "w trakcie",
            "zamknięte" => "zamknięte",
            _ => "nowe"
        };
    }
}
