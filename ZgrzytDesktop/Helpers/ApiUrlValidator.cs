using System;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Security;

namespace ZgrzytDesktop.Helpers;

public static class ApiUrlValidator
{
    public static string? Validate(string? apiBaseUrl, bool allowLocalHttpInDevMode = false)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            return null;

        var trimmed = apiBaseUrl.Trim();

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return AppStrings.Get("Security_ApiUrlMustBeHttps");

        if (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return null;

        if (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            if (allowLocalHttpInDevMode && ApiUrlSecurityHelper.IsHttpAllowedForHost(uri.Host))
                return null;

            return AppStrings.Get("Security_ApiUrlMustBeHttps");
        }

        return AppStrings.Get("Security_ApiUrlMustBeHttps");
    }

    public static bool IsValid(string? apiBaseUrl, bool allowLocalHttpInDevMode = false) =>
        Validate(apiBaseUrl, allowLocalHttpInDevMode) is null;
}
