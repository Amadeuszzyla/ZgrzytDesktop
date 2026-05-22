using System;

namespace ZgrzytDesktop.Constants;

public static class ApiDefaults
{
    public const string ProductionApiBaseUrl = "https://zgrzyt-api.onrender.com/api/";

    public static bool ShouldMigrateToProduction(string? apiBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            return true;

        var normalized = apiBaseUrl.Trim();

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
