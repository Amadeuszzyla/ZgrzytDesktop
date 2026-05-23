using System;
using System.Text.RegularExpressions;

namespace ZgrzytDesktop.Security;

/// <summary>
/// Prevents accidental logging of bearer tokens or JWT-like values.
/// </summary>
public static partial class SensitiveDataRedactor
{
    public static string Redact(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var redacted = BearerHeaderPattern().Replace(text, "Bearer [REDACTED]");
        redacted = JwtLikePattern().Replace(redacted, "[REDACTED_TOKEN]");

        return redacted;
    }

    public static bool ContainsSensitiveToken(string? text) =>
        !string.IsNullOrWhiteSpace(text) &&
        (BearerHeaderPattern().IsMatch(text) || JwtLikePattern().IsMatch(text));

    [GeneratedRegex(@"\bBearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BearerHeaderPattern();

    [GeneratedRegex(@"\beyJ[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\b", RegexOptions.CultureInvariant)]
    private static partial Regex JwtLikePattern();
}
