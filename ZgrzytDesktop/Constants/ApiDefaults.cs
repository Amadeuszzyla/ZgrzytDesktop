using System;

namespace ZgrzytDesktop.Constants;

public static class ApiDefaults
{
    public const string ProductionApiBaseUrl = "https://zgrzyt-api.onrender.com/api/";

    /// <summary>Poprzedni host produkcyjny — przy ładowaniu ustawień przechodzi na <see cref="ProductionApiBaseUrl"/>.</summary>
    public const string LegacyProductionApiHost = "zgrzyt-stolen-api.onrender.com";

    public static bool ShouldMigrateToProduction(string? apiBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            return true;

        var normalized = apiBaseUrl.Trim();

        if (normalized.Contains(LegacyProductionApiHost, StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalized.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalized.Contains("/admin/", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith("/admin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
