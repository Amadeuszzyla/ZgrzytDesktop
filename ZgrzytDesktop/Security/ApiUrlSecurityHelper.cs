using System;

namespace ZgrzytDesktop.Security;

public static class ApiUrlSecurityHelper
{
    public static string EnsureSecureApiBaseUrl(string apiBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            return apiBaseUrl;

        var trimmed = apiBaseUrl.Trim();

        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return trimmed;

        if (IsLocalDevelopmentHost(uri.Host))
            return trimmed;

        return "https://" + trimmed["http://".Length..];
    }

    public static bool IsHttpAllowedForHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        return IsLocalDevelopmentHost(host);
    }

    private static bool IsLocalDevelopmentHost(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        if (host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            return true;

        if (host.StartsWith("127.", StringComparison.Ordinal))
            return true;

        return host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);
    }
}
