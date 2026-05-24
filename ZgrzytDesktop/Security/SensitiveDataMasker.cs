using System;
using System.Text.RegularExpressions;

namespace ZgrzytDesktop.Security;

/// <summary>
/// Masks sensitive values before logging or persisting audit text.
/// </summary>
public static partial class SensitiveDataMasker
{
    public const string MaskedValue = "[MASKED]";
    public const string MaskedEmail = "[MASKED_EMAIL]";

    public static string Mask(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var masked = text;

        masked = SensitiveJsonFieldPattern().Replace(masked, match =>
        {
            var field = match.Groups[1].Value;
            return $"\"{field}\": \"[MASKED]\"";
        });
        masked = AuthorizationHeaderPattern().Replace(masked, "Authorization: [MASKED]");
        masked = BearerHeaderPattern().Replace(masked, "Bearer [MASKED]");
        masked = SetCookiePattern().Replace(masked, "Set-Cookie: [MASKED]");
        masked = CookieHeaderPattern().Replace(masked, "Cookie: [MASKED]");
        masked = EmailPattern().Replace(masked, MaskedEmail);
        masked = LoginInErrorPattern().Replace(masked, "login: [MASKED]");
        masked = SensitiveDataRedactor.Redact(masked);

        return masked;
    }

    public static bool IsSensitiveAuthRequestPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        return path.Contains("login", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("register", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("request-account", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ShouldMaskPlainText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return SensitiveFieldNamePattern().IsMatch(text) ||
               BearerHeaderPattern().IsMatch(text) ||
               EmailPattern().IsMatch(text) ||
               SensitiveDataRedactor.ContainsSensitiveToken(text);
    }

    [GeneratedRegex(@"""?(password|password_confirmation|token|refresh_token|access_token)""?\s*:\s*""([^""\\]|\\.)*""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveJsonFieldPattern();

    [GeneratedRegex(@"\b(password|password_confirmation|token|refresh_token|access_token)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveFieldNamePattern();

    [GeneratedRegex(@"\bAuthorization\s*:\s*[^\s\r\n]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AuthorizationHeaderPattern();

    [GeneratedRegex(@"\bBearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BearerHeaderPattern();

    [GeneratedRegex(@"\bSet-Cookie\s*:\s*[^\r\n]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SetCookiePattern();

    [GeneratedRegex(@"\bCookie\s*:\s*[^\r\n]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CookieHeaderPattern();

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\blogin\s*[:=]\s*[^\s,;""']+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LoginInErrorPattern();
}
